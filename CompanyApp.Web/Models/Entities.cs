using System.ComponentModel.DataAnnotations;

namespace CompanyApp.Web.Models;

public class Product
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [Range(0, 1_000_000)] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
    [EmailAddress] public string? Email { get; set; }
    [Phone] public string? Phone { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
