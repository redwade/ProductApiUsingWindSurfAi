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

public class ShippingServiceTests : IDisposable
{
    private readonly ProductDbContext _context;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ShippingService>> _loggerMock;
    private readonly ShippingService _shippingService;

    public ShippingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new ProductDbContext(options);
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ShippingService>>();
        
        _shippingService = new ShippingService(_context, _configurationMock.Object, _loggerMock.Object);
    }

    [Theory]
    [InlineData(ShippingProvider.USPS, ShippingSpeed.Standard, 5, 17.50)]
    [InlineData(ShippingProvider.FedEx, ShippingSpeed.Express, 10, 48.75)]
    [InlineData(ShippingProvider.UPS, ShippingSpeed.Overnight, 3, 52.50)]
    public void CalculateShippingCost_WithDifferentParameters_ShouldCalculateCorrectly(
        ShippingProvider provider, ShippingSpeed speed, decimal weight, decimal expectedCost)
    {
        // Act
        var cost = _shippingService.CalculateShippingCost(provider, speed, weight);

        // Assert
        cost.Should().Be(expectedCost);
    }

    [Fact]
    public async Task GetShippingRates_WithValidRequest_ShouldReturnAllRates()
    {
        // Arrange
        var request = new ShippingRateRequest
        {
            FromAddress = CreateValidAddress("Sender"),
            ToAddress = CreateValidAddress("Recipient"),
            Dimensions = new ShipmentDimensions
            {
                Length = 12,
                Width = 8,
                Height = 6,
                Weight = 5
            }
        };

        // Act
        var rates = await _shippingService.GetShippingRates(request);

        // Assert
        rates.Should().NotBeEmpty();
        rates.Should().HaveCount(16); // 4 providers × 4 speeds
        rates.Should().BeInAscendingOrder(r => r.Cost);
        rates.Should().AllSatisfy(r =>
        {
            r.Provider.Should().BeDefined();
            r.Speed.Should().BeDefined();
            r.Cost.Should().BeGreaterThan(0);
            r.EstimatedDays.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task CreateShipment_WithValidRequest_ShouldCreateShipment()
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
                Length = 10,
                Width = 8,
                Height = 6,
                Weight = 3
            }
        };

        // Act
        var response = await _shippingService.CreateShipment(request);

        // Assert
        response.Should().NotBeNull();
        response.ShipmentId.Should().BeGreaterThan(0);
        response.TrackingNumber.Should().NotBeNullOrEmpty();
        response.TrackingNumber.Should().StartWith("FX");
        response.Provider.Should().Be(ShippingProvider.FedEx);
        response.Status.Should().Be(ShipmentStatus.LabelGenerated);
        response.ShippingCost.Should().BeGreaterThan(0);
        response.LabelUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateShipment_WithInvalidFromAddress_ShouldThrowException()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            Provider = ShippingProvider.UPS,
            Speed = ShippingSpeed.Standard,
            FromAddress = new ShippingAddress { Name = "Test" }, // Incomplete
            ToAddress = CreateValidAddress("Customer"),
            Dimensions = new ShipmentDimensions { Length = 10, Width = 8, Height = 6, Weight = 5 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _shippingService.CreateShipment(request));
    }

    [Fact]
    public async Task CreateShipment_WithInvalidDimensions_ShouldThrowException()
    {
        // Arrange
        var request = new CreateShipmentRequest
        {
            Provider = ShippingProvider.USPS,
            Speed = ShippingSpeed.Standard,
            FromAddress = CreateValidAddress("Sender"),
            ToAddress = CreateValidAddress("Recipient"),
            Dimensions = new ShipmentDimensions { Length = 0, Width = 0, Height = 0, Weight = 0 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _shippingService.CreateShipment(request));
    }

    [Fact]
    public async Task GetShipment_WithValidId_ShouldReturnShipment()
    {
        // Arrange
        var request = CreateValidShipmentRequest();
        var created = await _shippingService.CreateShipment(request);

        // Act
        var shipment = await _shippingService.GetShipment(created.ShipmentId);

        // Assert
        shipment.Should().NotBeNull();
        shipment!.Id.Should().Be(created.ShipmentId);
        shipment.TrackingNumber.Should().Be(created.TrackingNumber);
    }

    [Fact]
    public async Task GetShipment_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var shipment = await _shippingService.GetShipment(99999);

        // Assert
        shipment.Should().BeNull();
    }

    [Fact]
    public async Task GetShipmentByTracking_WithValidTracking_ShouldReturnShipment()
    {
        // Arrange
        var request = CreateValidShipmentRequest();
        var created = await _shippingService.CreateShipment(request);

        // Act
        var shipment = await _shippingService.GetShipmentByTracking(created.TrackingNumber);

        // Assert
        shipment.Should().NotBeNull();
        shipment!.TrackingNumber.Should().Be(created.TrackingNumber);
    }

    [Fact]
    public async Task GetTrackingUpdates_WithValidTracking_ShouldReturnUpdates()
    {
        // Arrange
        var request = CreateValidShipmentRequest();
        var created = await _shippingService.CreateShipment(request);

        // Act
        var updates = await _shippingService.GetTrackingUpdates(created.TrackingNumber);

        // Assert
        updates.Should().NotBeEmpty();
        updates.Should().AllSatisfy(u =>
        {
            u.TrackingNumber.Should().Be(created.TrackingNumber);
            u.Status.Should().BeDefined();
            u.Timestamp.Should().BeAfter(DateTime.MinValue);
        });
    }

    [Fact]
    public async Task GetTrackingUpdates_WithInvalidTracking_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _shippingService.GetTrackingUpdates("INVALID123"));
    }

    [Fact]
    public async Task CancelShipment_WithValidShipment_ShouldCancelSuccessfully()
    {
        // Arrange
        var request = CreateValidShipmentRequest();
        var created = await _shippingService.CreateShipment(request);

        // Act
        var cancelled = await _shippingService.CancelShipment(created.ShipmentId);

        // Assert
        cancelled.Should().NotBeNull();
        cancelled.Status.Should().Be(ShipmentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelShipment_WithInvalidId_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _shippingService.CancelShipment(99999));
    }

    [Fact]
    public async Task GetShipmentHistory_WithNoFilter_ShouldReturnAllShipments()
    {
        // Arrange
        await _shippingService.CreateShipment(CreateValidShipmentRequest("user1@example.com"));
        await _shippingService.CreateShipment(CreateValidShipmentRequest("user2@example.com"));

        // Act
        var history = await _shippingService.GetShipmentHistory();

        // Assert
        history.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShipmentHistory_WithEmailFilter_ShouldReturnFilteredShipments()
    {
        // Arrange
        await _shippingService.CreateShipment(CreateValidShipmentRequest("user1@example.com"));
        await _shippingService.CreateShipment(CreateValidShipmentRequest("user2@example.com"));

        // Act
        var history = await _shippingService.GetShipmentHistory("user1@example.com");

        // Assert
        history.Should().HaveCount(1);
        history[0].ToEmail.Should().Be("user1@example.com");
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
