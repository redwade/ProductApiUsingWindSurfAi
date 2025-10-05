using Microsoft.EntityFrameworkCore;
using Stripe;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Services;

public class StripePaymentService : IPaymentService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly string _stripeSecretKey;
    private readonly string? _stripePublishableKey;

    public StripePaymentService(
        ProductDbContext context,
        IConfiguration configuration,
        ILogger<StripePaymentService> logger)
    {
        _context = context;
        _logger = logger;
        
        // Get Stripe keys from environment or configuration
        _stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                          ?? configuration["Stripe:SecretKey"]
                          ?? string.Empty;
        
        _stripePublishableKey = Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY")
                               ?? configuration["Stripe:PublishableKey"];

        if (string.IsNullOrEmpty(_stripeSecretKey))
        {
            _logger.LogWarning("Stripe secret key not configured. Payment features will use mock mode.");
        }
        else
        {
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }
    }

    public async Task<PaymentResponse> CreatePaymentIntent(PaymentRequest request)
    {
        // Validate product exists
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            throw new ArgumentException($"Product with ID {request.ProductId} not found");
        }

        // Calculate total amount
        var totalAmount = await CalculateTotalAmount(request.ProductId, request.Quantity);
        var amountInCents = (long)(totalAmount * 100); // Stripe uses cents

        if (string.IsNullOrEmpty(_stripeSecretKey))
        {
            // Mock mode for testing without API key
            return await CreateMockPaymentIntent(product, totalAmount, request);
        }

        try
        {
            // Create Stripe Payment Intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = request.Currency.ToLower(),
                Description = $"Purchase of {product.Name} (x{request.Quantity})",
                ReceiptEmail = request.CustomerEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "product_id", product.Id.ToString() },
                    { "product_name", product.Name },
                    { "quantity", request.Quantity.ToString() }
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            // Save to database
            var dbPaymentIntent = new Models.PaymentIntent
            {
                StripePaymentIntentId = paymentIntent.Id,
                ProductId = product.Id,
                Amount = totalAmount,
                Currency = request.Currency,
                Status = paymentIntent.Status,
                CustomerEmail = request.CustomerEmail,
                CustomerName = request.CustomerName,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentIntents.Add(dbPaymentIntent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment intent created: {PaymentIntentId} for product {ProductId}", 
                paymentIntent.Id, product.Id);

            return new PaymentResponse
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Amount = totalAmount,
                Currency = request.Currency,
                Status = paymentIntent.Status,
                PublishableKey = _stripePublishableKey
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent");
            throw new Exception($"Payment processing error: {ex.Message}", ex);
        }
    }

    public async Task<PaymentConfirmation> ConfirmPayment(string paymentIntentId)
    {
        var dbPayment = await _context.PaymentIntents
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

        if (dbPayment == null)
        {
            throw new ArgumentException($"Payment intent {paymentIntentId} not found");
        }

        if (string.IsNullOrEmpty(_stripeSecretKey))
        {
            // Mock mode
            dbPayment.Status = "succeeded";
            dbPayment.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new PaymentConfirmation
            {
                PaymentIntentId = paymentIntentId,
                Status = "succeeded",
                Amount = dbPayment.Amount,
                ReceiptUrl = $"https://mock-receipt.stripe.com/{paymentIntentId}"
            };
        }

        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            // Update database
            dbPayment.Status = paymentIntent.Status;
            if (paymentIntent.Status == "succeeded")
            {
                dbPayment.CompletedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment confirmed: {PaymentIntentId} with status {Status}", 
                paymentIntentId, paymentIntent.Status);

            return new PaymentConfirmation
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = dbPayment.Amount,
                ReceiptUrl = paymentIntent.Charges?.Data?.FirstOrDefault()?.ReceiptUrl
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error confirming payment");
            throw new Exception($"Payment confirmation error: {ex.Message}", ex);
        }
    }

    public async Task<PaymentConfirmation> CancelPayment(string paymentIntentId)
    {
        var dbPayment = await _context.PaymentIntents
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

        if (dbPayment == null)
        {
            throw new ArgumentException($"Payment intent {paymentIntentId} not found");
        }

        if (string.IsNullOrEmpty(_stripeSecretKey))
        {
            // Mock mode
            dbPayment.Status = "canceled";
            await _context.SaveChangesAsync();

            return new PaymentConfirmation
            {
                PaymentIntentId = paymentIntentId,
                Status = "canceled",
                Amount = dbPayment.Amount
            };
        }

        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.CancelAsync(paymentIntentId);

            // Update database
            dbPayment.Status = "canceled";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment canceled: {PaymentIntentId}", paymentIntentId);

            return new PaymentConfirmation
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = dbPayment.Amount
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error canceling payment");
            throw new Exception($"Payment cancellation error: {ex.Message}", ex);
        }
    }

    public async Task<Models.PaymentIntent?> GetPaymentStatus(string paymentIntentId)
    {
        return await _context.PaymentIntents
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
    }

    public async Task<List<Models.PaymentIntent>> GetPaymentHistory(string? customerEmail = null)
    {
        var query = _context.PaymentIntents.Include(p => p.Product).AsQueryable();

        if (!string.IsNullOrEmpty(customerEmail))
        {
            query = query.Where(p => p.CustomerEmail == customerEmail);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> CalculateTotalAmount(int productId, int quantity)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            throw new ArgumentException($"Product with ID {productId} not found");
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0");
        }

        return product.Price * quantity;
    }

    private async Task<PaymentResponse> CreateMockPaymentIntent(
        Models.Product product, 
        decimal totalAmount, 
        PaymentRequest request)
    {
        var mockPaymentIntentId = $"pi_mock_{Guid.NewGuid():N}";
        var mockClientSecret = $"{mockPaymentIntentId}_secret_{Guid.NewGuid():N}";

        var dbPaymentIntent = new Models.PaymentIntent
        {
            StripePaymentIntentId = mockPaymentIntentId,
            ProductId = product.Id,
            Amount = totalAmount,
            Currency = request.Currency,
            Status = "requires_payment_method",
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentIntents.Add(dbPaymentIntent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Mock payment intent created: {PaymentIntentId} for product {ProductId}", 
            mockPaymentIntentId, product.Id);

        return new PaymentResponse
        {
            PaymentIntentId = mockPaymentIntentId,
            ClientSecret = mockClientSecret,
            Amount = totalAmount,
            Currency = request.Currency,
            Status = "requires_payment_method",
            PublishableKey = "pk_test_mock_key"
        };
    }
}
