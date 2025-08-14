using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CompanyApp.Web.Data;
using CompanyApp.Web.Models;

namespace CompanyApp.Web.Pages.Orders;

public class InvoiceModel : PageModel
{
    private readonly AppDbContext _db;
    public InvoiceModel(AppDbContext db) => _db = db;

    public Order? Order { get; set; }
    public decimal Total => Order?.Items.Sum(i => i.Quantity * i.UnitPrice) ?? 0m;

    public async Task<IActionResult> OnGetAsync(int id, bool generate = false)
    {
        Order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (Order == null) return NotFound();

        if (generate && string.IsNullOrEmpty(Order.InvoiceNumber))
        {
            var now = DateTime.UtcNow;
            var year = now.Year;

            // простой автонумератор в рамках года
            var countThisYear = await _db.Orders
                .Where(o => o.InvoiceNumber != null && o.InvoiceDate.HasValue && o.InvoiceDate.Value.Year == year)
                .CountAsync();

            Order.InvoiceDate = now;
            Order.InvoiceNumber = $"INV-{year}-{(countThisYear + 1):D4}";
            await _db.SaveChangesAsync();
        }

        return Page();
    }
}
