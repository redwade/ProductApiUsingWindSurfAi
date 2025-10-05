using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Tests.IntegrationTests;

public class PaymentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PaymentApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ProductDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreatePayment_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var paymentRequest = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 2,
            CustomerEmail = "test@example.com",
            CustomerName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.PaymentIntentId.Should().NotBeNullOrEmpty();
        paymentResponse.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreatePayment_WithInvalidProduct_ShouldReturnBadRequest()
    {
        // Arrange
        var paymentRequest = new PaymentRequest
        {
            ProductId = 999,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmPayment_WithValidPaymentIntent_ShouldReturnOk()
    {
        // Arrange - Create a payment first
        var paymentRequest = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act
        var response = await _client.PostAsync($"/api/payments/{payment!.PaymentIntentId}/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmation = await response.Content.ReadFromJsonAsync<PaymentConfirmation>();
        confirmation.Should().NotBeNull();
        confirmation!.Status.Should().Be("succeeded");
    }

    [Fact]
    public async Task ConfirmPayment_WithInvalidPaymentIntent_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/payments/invalid_id/confirm", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelPayment_WithValidPaymentIntent_ShouldReturnOk()
    {
        // Arrange - Create a payment first
        var paymentRequest = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act
        var response = await _client.PostAsync($"/api/payments/{payment!.PaymentIntentId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmation = await response.Content.ReadFromJsonAsync<PaymentConfirmation>();
        confirmation.Should().NotBeNull();
        confirmation!.Status.Should().Be("canceled");
    }

    [Fact]
    public async Task GetPaymentStatus_WithValidPaymentIntent_ShouldReturnPayment()
    {
        // Arrange - Create a payment first
        var paymentRequest = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "test@example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Act
        var response = await _client.GetAsync($"/api/payments/{payment!.PaymentIntentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paymentIntent = await response.Content.ReadFromJsonAsync<PaymentIntent>();
        paymentIntent.Should().NotBeNull();
        paymentIntent!.StripePaymentIntentId.Should().Be(payment.PaymentIntentId);
    }

    [Fact]
    public async Task GetPaymentStatus_WithInvalidPaymentIntent_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/payments/invalid_id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaymentHistory_ShouldReturnAllPayments()
    {
        // Arrange - Create multiple payments
        await _client.PostAsJsonAsync("/api/payments/create", new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "user1@example.com"
        });
        await _client.PostAsJsonAsync("/api/payments/create", new PaymentRequest
        {
            ProductId = 2,
            Quantity = 1,
            CustomerEmail = "user2@example.com"
        });

        // Act
        var response = await _client.GetAsync("/api/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentIntent>>();
        payments.Should().NotBeNull();
        payments!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetPaymentHistory_WithEmailFilter_ShouldReturnFilteredPayments()
    {
        // Arrange - Create payments for different users
        await _client.PostAsJsonAsync("/api/payments/create", new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "specific@example.com"
        });

        // Act
        var response = await _client.GetAsync("/api/payments?customerEmail=specific@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentIntent>>();
        payments.Should().NotBeNull();
        payments!.Should().AllSatisfy(p => p.CustomerEmail.Should().Be("specific@example.com"));
    }

    [Theory]
    [InlineData(1, 1, 199.99)]
    [InlineData(1, 2, 399.98)]
    [InlineData(2, 3, 899.97)]
    public async Task CalculatePrice_WithDifferentQuantities_ShouldReturnCorrectTotal(
        int productId, int quantity, decimal expectedTotal)
    {
        // Act
        var response = await _client.GetAsync($"/api/products/{productId}/calculate-price?quantity={quantity}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedTotal.ToString("F2"));
    }

    [Fact]
    public async Task CalculatePrice_WithInvalidProduct_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/products/999/calculate-price?quantity=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaymentWorkflow_CreateConfirmCancel_ShouldWorkEndToEnd()
    {
        // Step 1: Create payment
        var paymentRequest = new PaymentRequest
        {
            ProductId = 1,
            Quantity = 1,
            CustomerEmail = "workflow@example.com",
            CustomerName = "Workflow Test"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // Step 2: Check status
        var statusResponse = await _client.GetAsync($"/api/payments/{payment!.PaymentIntentId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Cancel payment
        var cancelResponse = await _client.PostAsync($"/api/payments/{payment.PaymentIntentId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancellation = await cancelResponse.Content.ReadFromJsonAsync<PaymentConfirmation>();
        cancellation!.Status.Should().Be("canceled");
    }
}
