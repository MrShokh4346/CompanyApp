using System;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Web.Data;
using CompanyApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CompanyApp.Web.Pages.Orders
{
    [Authorize]
    public class InvoiceModel : PageModel
    {
        private readonly AppDbContext _db;
        public InvoiceModel(AppDbContext db) { _db = db; }
        public Order? Order { get; set; }
        public decimal Total => Order?.Items.Sum(i => i.Quantity * i.UnitPrice) ?? 0m;
        public decimal Paid  => Order?.Payments.Sum(p => p.Amount) ?? 0m;
        public decimal Due   => Math.Max(0, Total - Paid);

        public async Task<IActionResult> OnGet(int id, bool generate = false, string? post = null)
        {
            Order = await _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Payments) // 👈 важно
                .FirstOrDefaultAsync(o => o.Id == id);

            if (Order == null) return NotFound();

            // Сгенерировать номер/дату накладной (один раз)
            if (generate && string.IsNullOrWhiteSpace(Order.InvoiceNumber))
            {
                Order.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Order.Id:D5}";
                Order.InvoiceDate = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // Провести ПРИХОД (на склад) по накладной
            if (post == "receipt")
            {
                var exists = await _db.Receipts.FirstOrDefaultAsync(r => r.OrderId == Order.Id);
                if (exists == null)
                {
                    var r = new Receipt
                    {
                        OrderId = Order.Id,
                        Date = DateTime.UtcNow,
                        Number = $"PRC-{DateTime.UtcNow:yyyyMMddHHmm}-{Order.Id}",
                        Supplier = Order.Customer?.FullName
                    };
                    foreach (var it in Order.Items)
                    {
                        r.Items.Add(new ReceiptItem
                        {
                            ProductId = it.ProductId,
                            Quantity = it.Quantity,
                            UnitPrice = it.UnitPrice
                        });
                    }
                    _db.Receipts.Add(r);
                    await _db.SaveChangesAsync();

                    // сразу создаём движения "Приход"
                    foreach (var it in r.Items)
                    {
                        _db.StockMoves.Add(new StockMove
                        {
                            ProductId = it.ProductId,
                            Quantity = it.Quantity,
                            UnitPrice = it.UnitPrice,
                            Type = StockMoveType.In,
                            RefReceiptId = r.Id
                        });
                    }
                    await _db.SaveChangesAsync();

                    TempData["Ok"] = $"Приход по накладной проведён (№ {r.Number}).";
                }
                else
                {
                    TempData["Ok"] = $"Приход уже существует (№ {exists.Number} от {exists.Date:dd.MM.yyyy}).";
                }
            }

            // Провести РАСХОД (списать со склада) по заказу
            if (post == "issue")
            {
                var already = await _db.StockMoves
                    .AnyAsync(m => m.RefOrderId == Order.Id && m.Type == StockMoveType.Out);

                if (!already)
                {
                    foreach (var it in Order.Items)
                    {
                        _db.StockMoves.Add(new StockMove
                        {
                            ProductId = it.ProductId,
                            Quantity = it.Quantity,
                            UnitPrice = it.UnitPrice,
                            Type = StockMoveType.Out,
                            RefOrderId = Order.Id
                        });
                    }
                    await _db.SaveChangesAsync();
                    TempData["Ok"] = "Расход по заказу проведён (товары списаны со склада).";
                }
                else
                {
                    TempData["Ok"] = "Расход по этому заказу уже был проведён ранее.";
                }
            }

            return Page();
        }
    }
}
