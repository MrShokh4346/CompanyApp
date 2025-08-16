using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Web.Data;
using CompanyApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CompanyApp.Web.Pages.Warehouse
{
    [Authorize]
    public class StockModel : PageModel
    {
        private readonly AppDbContext _db;
        public StockModel(AppDbContext db) { _db = db; }

        public class Row
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public int OnHand { get; set; }
            public int InQty { get; set; }
            public int OutQty { get; set; }
        }

        public List<Row> Items { get; set; } = new();

        public async Task OnGet()
        {
            var products = await _db.Products.OrderBy(p => p.Name).ToListAsync();
            var moves = await _db.StockMoves.AsNoTracking().ToListAsync();

            var dict = products.ToDictionary(p => p.Id, p => new Row { ProductId = p.Id, Name = p.Name });

            foreach (var m in moves)
            {
                if (!dict.TryGetValue(m.ProductId, out var r)) continue;
                if (m.Type == StockMoveType.In) r.InQty += m.Quantity;
                else if (m.Type == StockMoveType.Out) r.OutQty += m.Quantity;
            }
            foreach (var r in dict.Values)
                r.OnHand = r.InQty - r.OutQty;

            Items = dict.Values.OrderBy(r => r.Name).ToList();
        }
    }
}
