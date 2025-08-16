using System.Text;
using System.IO;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CompanyApp.Web.Data;
using System.Globalization;
using CompanyApp.Web.Pdf;      // ← ЭТО ДОБАВИТЬ
using QuestPDF.Fluent;     
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

var culture = new CultureInfo("ru-RU");
culture.NumberFormat.CurrencySymbol = "сум";      // будет: 12 345,67 сум
culture.NumberFormat.CurrencyDecimalDigits = 2;    // если хочешь без копеек — поставь 0
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Identity (cookie для UI)
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Куки: явно зададим путь логина
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.SlidingExpiration = true;
});

// Razor Pages: всё закрыто, кроме страницы логина
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");
    o.Conventions.AllowAnonymousToPage("/Account/Login");
    // Если нужна открытая главная — раскомментируй:
    // o.Conventions.AllowAnonymousToPage("/Index");
});

// JWT для API (НЕ схема по умолчанию)
var jwt = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder.Services.AddAuthentication() // по умолчанию остаётся cookie от Identity
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = key
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Swagger (только в Dev)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Доверяем заголовкам прокси (Cloudflare/ngrok) — ДО auth/redirects!
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// PWA service worker (из wwwroot/pwa/service-worker.js)
app.MapWhen(ctx => ctx.Request.Path == "/pwa/service-worker.js", sw =>
{
    sw.Run(async http =>
    {
        http.Response.ContentType = "application/javascript";
        await http.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "pwa", "service-worker.js"));
    });
});

app.MapControllers();
app.MapRazorPages();

app.MapGet("/orders/{id:int}/invoice.pdf", async (int id, AppDbContext db) =>
{
    var order = await db.Orders
        .Include(o => o.Customer)
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .Include(o => o.Payments)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order == null) return Results.NotFound();

    var fileName = $"Invoice-{(order.InvoiceNumber ?? id.ToString())}.pdf";

    var doc = new InvoiceDocument(order);
    var pdfBytes = doc.GeneratePdf(); // QuestPDF

    // attachment -> принудительное скачивание на телефонах
    return Results.File(pdfBytes, "application/pdf", fileName);
}).RequireAuthorization();

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    const string adminEmail = "admin@local";
    const string adminPass = "Admin123$";

    if (!await roleMgr.RoleExistsAsync("Admin"))
        await roleMgr.CreateAsync(new IdentityRole("Admin"));

    var admin = await userMgr.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        await userMgr.CreateAsync(admin, adminPass);
        await userMgr.AddToRoleAsync(admin, "Admin");
    }
}

app.Run();
