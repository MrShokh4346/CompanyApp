using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Web.Data;
using CompanyApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CompanyApp.Web.Pages.Finance
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db) { _db = db; }

        public string View { get; set; } = "income";

        public record OrderRow(
            int Id,
            string? InvoiceNumber,
            DateTime Date,
            string Customer,
            decimal Total,
            decimal Paid,
            decimal Due
        );

        public record PurchaseRow(
            int Id,
            string Number,
            DateTime Date,
            string Supplier,
            decimal Total,
            decimal Paid,
            decimal Due
        );

        public List<OrderRow> Income { get; set; } = new();
        public List<PurchaseRow> Expenses { get; set; } = new();
        public List<OrderRow> Debtors { get; set; } = new();
        public List<PurchaseRow> Creditors { get; set; } = new();

        // 👇 агрегаты для «общей суммы» по каждой вкладке
        public decimal IncomeTotal { get; set; }
        public decimal IncomePaid  { get; set; }
        public decimal IncomeDue   { get; set; }

        public decimal ExpensesTotal { get; set; }
        public decimal ExpensesPaid  { get; set; }
        public decimal ExpensesDue   { get; set; }

        public decimal DebtorsDue    { get; set; }
        public decimal CreditorsDue  { get; set; }

        public async Task OnGet(string? view = null)
        {
            View = string.IsNullOrWhiteSpace(view) ? "income" : view.ToLowerInvariant();

            // ---- ПРОДАЖИ (заказы) ----
            var orders = await _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            Income = orders.Select(o =>
            {
                var total = o.Items.Sum(i => i.UnitPrice * i.Quantity);
                var paid  = o.Payments.Sum(p => p.Amount);
                var due   = Math.Max(0, total - paid);
                return new OrderRow(
                    o.Id,
                    o.InvoiceNumber,
                    o.CreatedAt,
                    o.Customer?.FullName ?? "—",
                    total, paid, due
                );
            }).ToList();

            Debtors = Income.Where(r => r.Due > 0)
                            .OrderByDescending(r => r.Due)
                            .ToList();

            // ---- ЗАКУПКИ (приходные накладные) ----
            var pinvs = await _db.PurchaseInvoices
                .Include(pi => pi.Supplier)
                .Include(pi => pi.Items)
                .Include(pi => pi.Payments)
                .OrderByDescending(pi => pi.Date)
                .ToListAsync();

            Expenses = pinvs.Select(pi =>
            {
                var total = pi.Items.Sum(i => i.UnitCost * i.Quantity);
                var paid  = pi.Payments.Sum(p => p.Amount);
                var due   = Math.Max(0, total - paid);
                return new PurchaseRow(
                    pi.Id,
                    pi.Number,
                    pi.Date,
                    pi.Supplier.Name,
                    total, paid, due
                );
            }).ToList();

            Creditors = Expenses.Where(r => r.Due > 0)
                                .OrderByDescending(r => r.Due)
                                .ToList();

            // ---- агрегаты ----
            IncomeTotal = Income.Sum(x => x.Total);
            IncomePaid  = Income.Sum(x => x.Paid);
            IncomeDue   = Income.Sum(x => x.Due);

            ExpensesTotal = Expenses.Sum(x => x.Total);
            ExpensesPaid  = Expenses.Sum(x => x.Paid);
            ExpensesDue   = Expenses.Sum(x => x.Due);

            DebtorsDue   = Debtors.Sum(x => x.Due);
            CreditorsDue = Creditors.Sum(x => x.Due);
        }
    }
}
