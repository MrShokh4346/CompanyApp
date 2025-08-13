using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompanyApp.Web.Data;
using CompanyApp.Web.Models;

namespace CompanyApp.Web.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> Get() => await db.Products.AsNoTracking().ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> Get(int id) => await db.Products.FindAsync(id) is Product p ? p : NotFound();

    [HttpPost]
    public async Task<ActionResult<Product>> Post(Product p)
    {
        db.Products.Add(p); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, Product p)
    {
        if (id!=p.Id) return BadRequest();
        db.Entry(p).State = EntityState.Modified; await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Products.FindAsync(id); if (p==null) return NotFound();
        db.Products.Remove(p); await db.SaveChangesAsync();
        return NoContent();
    }
}
