using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentIntent(PaymentRequest request);
    Task<PaymentConfirmation> ConfirmPayment(string paymentIntentId);
    Task<PaymentConfirmation> CancelPayment(string paymentIntentId);
    Task<PaymentIntent?> GetPaymentStatus(string paymentIntentId);
    Task<List<PaymentIntent>> GetPaymentHistory(string? customerEmail = null);
    Task<decimal> CalculateTotalAmount(int productId, int quantity);
}
