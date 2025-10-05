namespace WindsurfProductAPI.Models;

public class PaymentRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Currency { get; set; } = "inr";
}

public class PaymentResponse
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PublishableKey { get; set; }
}

public class PaymentConfirmation
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReceiptUrl { get; set; }
}
