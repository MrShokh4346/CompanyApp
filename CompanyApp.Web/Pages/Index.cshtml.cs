using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CompanyApp.Web.Data;

namespace CompanyApp.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) { _db = db; }

    public string LabelsJson { get; set; } = "[]";
    public string ValuesJson { get; set; } = "[]";
    public decimal Total30 { get; set; }
    public int OrdersCount { get; set; }
    public int ProductsCount { get; set; }

    public async Task OnGet()
    {
        var from = DateTime.UtcNow.Date.AddDays(-29);               // сегодня + 29 дней назад
        var days = Enumerable.Range(0, 30).Select(d => from.AddDays(d)).ToList();

        var orderTotals = await _db.Orders
            .Where(o => o.CreatedAt >= from)
            .Select(o => new {
                Day = o.CreatedAt.Date,
                Total = o.Items.Sum(i => (decimal?)(i.Quantity * i.UnitPrice)) ?? 0m
            })
            .ToListAsync();

        var grouped = orderTotals
            .GroupBy(x => x.Day)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Total));

        var labels = days.Select(d => d.ToString("dd.MM")).ToList();
        var values = days.Select(d => grouped.TryGetValue(d, out var s) ? decimal.Round(s, 2) : 0m).ToList();

        LabelsJson = JsonSerializer.Serialize(labels);
        ValuesJson = JsonSerializer.Serialize(values);
        Total30 = values.Sum();
        OrdersCount = await _db.Orders.CountAsync();
        ProductsCount = await _db.Products.CountAsync();
    }
}
