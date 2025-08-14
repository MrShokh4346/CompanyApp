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

    // --- Для накладной ---
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }

    // --- Платежи по заказу ---
    public List<Payment> Payments { get; set; } = new();
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

// Поступившие деньги по заказам (для дебиторов/доходов)
public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = default!;
    [Range(0, 1_000_000_000)] public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    [MaxLength(200)] public string? Method { get; set; } // Нал, карта, банк
    [MaxLength(500)] public string? Note { get; set; }
}

// Счета/расходы (кредиторы)
public class Expense
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Vendor { get; set; } = string.Empty; // Поставщик
    [MaxLength(300)] public string? Description { get; set; }
    [Range(0, 1_000_000_000)] public decimal Amount { get; set; }
    [Range(0, 1_000_000_000)] public decimal PaidAmount { get; set; } = 0m;
    public DateTime? DueDate { get; set; }      // Срок оплаты
    public DateTime? PaidAt { get; set; }       // Когда закрыт (если оплачен)
    public bool IsPaid => PaidAmount >= Amount - 0.01m;
    public decimal Outstanding => Math.Max(0, Amount - PaidAmount);
}
