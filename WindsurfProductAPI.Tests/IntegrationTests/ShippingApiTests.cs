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

public class ShippingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ShippingApiTests(WebApplicationFactory<Program> factory)
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
    public async Task GetShippingRates_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new ShippingRateRequest
        {
            FromAddress = CreateValidAddress("Warehouse"),
            ToAddress = CreateValidAddress("Customer"),
            Dimensions = new ShipmentDimensions
            {
                Length = 12,
                Width = 10,
                Height = 8,
                Weight = 5
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipping/rates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rates = await response.Content.ReadFromJsonAsync<List<ShippingRate>>();
        rates.Should().NotBeEmpty();
        rates.Should().AllSatisfy(r => r.Cost.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task GetShippingRates_WithInvalidAddress_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ShippingRateRequest
        {
            FromAddress = new ShippingAddress { Name = "Test" }, // Incomplete
            ToAddress = CreateValidAddress("Customer"),
            Dimensions = new ShipmentDimensions { Length = 10, Width = 8, Height = 6, Weight = 5 }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipping/rates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateShipment_WithValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            Provider = ShippingProvider.FedEx,
            Speed = ShippingSpeed.Express,
            FromAddress = CreateValidAddress("Warehouse"),
            ToAddress = CreateValidAddress("Customer"),
            Dimensions = new ShipmentDimensions
            {
                Length = 12,
                Width = 10,
                Height = 8,
                Weight = 5
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/shipping/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var shipment = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
        shipment.Should().NotBeNull();
        shipment!.TrackingNumber.Should().NotBeNullOrEmpty();
        shipment.ShippingCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetShipment_WithValidId_ShouldReturnOk()
    {
        // Arrange - Create a shipment first
        var createRequest = CreateValidShipmentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/shipping/create", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();

        // Act
        var response = await _client.GetAsync($"/api/shipping/{created!.ShipmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        shipment.Should().NotBeNull();
        shipment!.Id.Should().Be(created.ShipmentId);
    }

    [Fact]
    public async Task GetShipment_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/shipping/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShipmentByTracking_WithValidTracking_ShouldReturnOk()
    {
        // Arrange - Create a shipment first
        var createRequest = CreateValidShipmentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/shipping/create", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();

        // Act
        var response = await _client.GetAsync($"/api/shipping/track/{created!.TrackingNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        shipment.Should().NotBeNull();
        shipment!.TrackingNumber.Should().Be(created.TrackingNumber);
    }

    [Fact]
    public async Task GetTrackingUpdates_WithValidTracking_ShouldReturnOk()
    {
        // Arrange - Create a shipment first
        var createRequest = CreateValidShipmentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/shipping/create", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();

        // Act
        var response = await _client.GetAsync($"/api/shipping/track/{created!.TrackingNumber}/updates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updates = await response.Content.ReadFromJsonAsync<List<TrackingUpdate>>();
        updates.Should().NotBeEmpty();
        updates.Should().AllSatisfy(u => u.TrackingNumber.Should().Be(created.TrackingNumber));
    }

    [Fact]
    public async Task GetTrackingUpdates_WithInvalidTracking_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/shipping/track/INVALID123/updates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelShipment_WithValidId_ShouldReturnOk()
    {
        // Arrange - Create a shipment first
        var createRequest = CreateValidShipmentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/shipping/create", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();

        // Act
        var response = await _client.PostAsync($"/api/shipping/{created!.ShipmentId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        shipment.Should().NotBeNull();
        shipment!.Status.Should().Be(ShipmentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelShipment_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/shipping/99999/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShipmentHistory_ShouldReturnAllShipments()
    {
        // Arrange - Create multiple shipments
        await _client.PostAsJsonAsync("/api/shipping/create", CreateValidShipmentRequest("user1@example.com"));
        await _client.PostAsJsonAsync("/api/shipping/create", CreateValidShipmentRequest("user2@example.com"));

        // Act
        var response = await _client.GetAsync("/api/shipping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipments = await response.Content.ReadFromJsonAsync<List<Shipment>>();
        shipments.Should().NotBeNull();
        shipments!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetShipmentHistory_WithEmailFilter_ShouldReturnFilteredShipments()
    {
        // Arrange - Create shipments for different users
        await _client.PostAsJsonAsync("/api/shipping/create", CreateValidShipmentRequest("specific@example.com"));
        await _client.PostAsJsonAsync("/api/shipping/create", CreateValidShipmentRequest("other@example.com"));

        // Act
        var response = await _client.GetAsync("/api/shipping?customerEmail=specific@example.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shipments = await response.Content.ReadFromJsonAsync<List<Shipment>>();
        shipments.Should().NotBeNull();
        shipments!.Should().AllSatisfy(s => s.ToEmail.Should().Be("specific@example.com"));
    }

    [Theory]
    [InlineData("FedEx", "Standard", 5, 17.50)]
    [InlineData("UPS", "Express", 10, 45.50)]
    [InlineData("USPS", "Overnight", 3, 27.50)]
    public async Task CalculateShippingCost_WithDifferentParameters_ShouldReturnCorrectCost(
        string provider, string speed, decimal weight, decimal expectedCost)
    {
        // Act
        var response = await _client.GetAsync(
            $"/api/shipping/calculate-cost?provider={provider}&speed={speed}&weight={weight}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedCost.ToString("F2"));
    }

    [Fact]
    public async Task ShippingWorkflow_RatesCreateTrackCancel_ShouldWorkEndToEnd()
    {
        // Step 1: Get shipping rates
        var ratesRequest = new ShippingRateRequest
        {
            FromAddress = CreateValidAddress("Warehouse"),
            ToAddress = CreateValidAddress("Customer"),
            Dimensions = new ShipmentDimensions { Length = 12, Width = 10, Height = 8, Weight = 5 }
        };
        var ratesResponse = await _client.PostAsJsonAsync("/api/shipping/rates", ratesRequest);
        ratesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Create shipment
        var createRequest = CreateValidShipmentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/shipping/create", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var shipment = await createResponse.Content.ReadFromJsonAsync<ShipmentResponse>();

        // Step 3: Track shipment
        var trackResponse = await _client.GetAsync($"/api/shipping/track/{shipment!.TrackingNumber}");
        trackResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Get tracking updates
        var updatesResponse = await _client.GetAsync($"/api/shipping/track/{shipment.TrackingNumber}/updates");
        updatesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Cancel shipment
        var cancelResponse = await _client.PostAsync($"/api/shipping/{shipment.ShipmentId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private ShippingAddress CreateValidAddress(string name)
    {
        return new ShippingAddress
        {
            Name = name,
            Street1 = "123 Main St",
            City = "San Francisco",
            State = "CA",
            PostalCode = "94102",
            Country = "US",
            Phone = "555-1234",
            Email = $"{name.ToLower()}@example.com"
        };
    }

    private CreateShipmentRequest CreateValidShipmentRequest(string? email = null)
    {
        var toAddress = CreateValidAddress("Customer");
        if (email != null)
        {
            toAddress.Email = email;
        }

        return new CreateShipmentRequest
        {
            Provider = ShippingProvider.FedEx,
            Speed = ShippingSpeed.Standard,
            FromAddress = CreateValidAddress("Warehouse"),
            ToAddress = toAddress,
            Dimensions = new ShipmentDimensions
            {
                Length = 12,
                Width = 10,
                Height = 8,
                Weight = 5
            }
        };
    }
}
