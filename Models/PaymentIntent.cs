namespace WindsurfProductAPI.Models;

public class PaymentIntent
{
    public int Id { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = "pending"; // pending, succeeded, failed, canceled
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}
