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
    public class ReceiptsModel : PageModel
    {
        private readonly AppDbContext _db;
        public ReceiptsModel(AppDbContext db) { _db = db; }

        public List<Receipt> Items { get; set; } = new();

        public async Task OnGet()
        {
            Items = await _db.Receipts
                .Include(r => r.Order)
                .Include(r => r.Items).ThenInclude(i => i.Product)
                .OrderByDescending(r => r.Date)
                .Take(200)
                .ToListAsync();
        }
    }
}
