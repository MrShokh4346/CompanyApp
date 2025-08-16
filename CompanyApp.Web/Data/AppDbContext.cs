using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CompanyApp.Web.Models;

namespace CompanyApp.Web.Data;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();
    public DbSet<StockMove> StockMoves => Set<StockMove>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<PurchasePayment> PurchasePayments => Set<PurchasePayment>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<PurchaseInvoice>()
            .HasMany(x => x.Items)
            .WithOne(x => x.PurchaseInvoice)
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<PurchaseInvoice>()
            .HasMany(x => x.Payments)
            .WithOne(x => x.PurchaseInvoice)
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

            // Receipt может ссылаться на PurchaseInvoice (необязательно)
        b.Entity<Receipt>()
            .HasOne(r => r.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(r => r.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<StockMove>()
            .HasIndex(x => new { x.ProductId, x.Type, x.CreatedAt });
        b.Entity<Receipt>()
            .HasMany(r => r.Items)
            .WithOne(i => i.Receipt)
            .HasForeignKey(i => i.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        b.Entity<OrderItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);

        // NEW: ден. поля
        b.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
        b.Entity<Expense>().Property(e => e.Amount).HasPrecision(18, 2);
        b.Entity<Expense>().Property(e => e.PaidAmount).HasPrecision(18, 2);

        // связи
        b.Entity<OrderItem>()
            .HasOne(i => i.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.OrderId);
        b.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId);

        b.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId);
    }
}
