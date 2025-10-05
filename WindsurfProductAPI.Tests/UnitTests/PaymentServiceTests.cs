using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;
using WindsurfProductAPI.Services;

namespace WindsurfProductAPI.Tests.UnitTests;

public class PaymentServiceTests : IDisposable
{
    private readonly ProductDbContext _context;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<StripePaymentService>> _loggerMock;
    private readonly StripePaymentService _paymentService;

    public PaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new ProductDbContext(options);
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<StripePaymentService>>();
        
        // Setup mock configuration (no API key for testing)
        _configurationMock.Setup(c => c["Stripe:SecretKey"]).Returns((string?)null);
        
        _paymentService = new StripePaymentService(_context, _configurationMock.Object, _loggerMock.Object);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Products.AddRange(
            new Product { Id = 1, Name = "Test Product 1", Price = 100m, Category = "Electronics" },
            new Product { Id = 2, Name = "Test Product 2", Price = 50m, Category = "Books" }
        );
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreatePaymentIntent_WithValidProduct_ShouldCreatePayment()
    {
        // Arrange
        var request = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 2,
            CustomerEmail = "test@example.com",
            CustomerName = "Test User"
        };

        // Act
        var response = await _paymentService.CreatePaymentIntent(request);

        // Assert
        response.Should().NotBeNull();
        response.PaymentIntentId.Should().NotBeNullOrEmpty();
        response.ClientSecret.Should().NotBeNullOrEmpty();
        response.Amount.Should().Be(200m); // 100 * 2
        response.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePaymentIntent_WithInvalidProduct_ShouldThrowException()
    {
        // Arrange
        var request = new PaymentRequest
        {
            ProductId = 999,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _paymentService.CreatePaymentIntent(request));
    }

    [Theory]
    [InlineData(1, 1, 100)]
    [InlineData(1, 2, 200)]
    [InlineData(2, 3, 150)]
    public async Task CalculateTotalAmount_WithDifferentQuantities_ShouldCalculateCorrectly(
        int productId, int quantity, decimal expectedTotal)
    {
        // Act
        var total = await _paymentService.CalculateTotalAmount(productId, quantity);

        // Assert
        total.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task CalculateTotalAmount_WithZeroQuantity_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _paymentService.CalculateTotalAmount(1, 0));
    }

    [Fact]
    public async Task CalculateTotalAmount_WithNegativeQuantity_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _paymentService.CalculateTotalAmount(1, -5));
    }

    [Fact]
    public async Task ConfirmPayment_WithValidPaymentIntent_ShouldUpdateStatus()
    {
        // Arrange
        var request = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };
        var payment = await _paymentService.CreatePaymentIntent(request);

        // Act
        var confirmation = await _paymentService.ConfirmPayment(payment.PaymentIntentId);

        // Assert
        confirmation.Should().NotBeNull();
        confirmation.Status.Should().Be("succeeded");
        confirmation.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task ConfirmPayment_WithInvalidPaymentIntent_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _paymentService.ConfirmPayment("invalid_payment_intent_id"));
    }

    [Fact]
    public async Task CancelPayment_WithValidPaymentIntent_ShouldCancelPayment()
    {
        // Arrange
        var request = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };
        var payment = await _paymentService.CreatePaymentIntent(request);

        // Act
        var confirmation = await _paymentService.CancelPayment(payment.PaymentIntentId);

        // Assert
        confirmation.Should().NotBeNull();
        confirmation.Status.Should().Be("canceled");
    }

    [Fact]
    public async Task GetPaymentStatus_WithValidPaymentIntent_ShouldReturnPayment()
    {
        // Arrange
        var request = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };
        var payment = await _paymentService.CreatePaymentIntent(request);

        // Act
        var status = await _paymentService.GetPaymentStatus(payment.PaymentIntentId);

        // Assert
        status.Should().NotBeNull();
        status!.StripePaymentIntentId.Should().Be(payment.PaymentIntentId);
        status.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetPaymentStatus_WithInvalidPaymentIntent_ShouldReturnNull()
    {
        // Act
        var status = await _paymentService.GetPaymentStatus("invalid_id");

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentHistory_WithNoFilter_ShouldReturnAllPayments()
    {
        // Arrange
        await _paymentService.CreatePaymentIntent(new PaymentRequest 
        { 
            ProductId = 1, 
            Quantity = 1, 
            CustomerEmail = "user1@example.com" 
        });
        await _paymentService.CreatePaymentIntent(new PaymentRequest 
        { 
            ProductId = 2, 
            Quantity = 1, 
            CustomerEmail = "user2@example.com" 
        });

        // Act
        var history = await _paymentService.GetPaymentHistory();

        // Assert
        history.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPaymentHistory_WithEmailFilter_ShouldReturnFilteredPayments()
    {
        // Arrange
        await _paymentService.CreatePaymentIntent(new PaymentRequest 
        { 
            ProductId = 1, 
            Quantity = 1, 
            CustomerEmail = "user1@example.com" 
        });
        await _paymentService.CreatePaymentIntent(new PaymentRequest 
        { 
            ProductId = 2, 
            Quantity = 1, 
            CustomerEmail = "user2@example.com" 
        });

        // Act
        var history = await _paymentService.GetPaymentHistory("user1@example.com");

        // Assert
        history.Should().HaveCount(1);
        history[0].CustomerEmail.Should().Be("user1@example.com");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
