using Microsoft.EntityFrameworkCore;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Services;

public class ShippingService : IShippingService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ShippingService> _logger;
    private readonly Dictionary<string, string> _providerApiKeys;

    public ShippingService(
        ProductDbContext context,
        IConfiguration configuration,
        ILogger<ShippingService> logger)
    {
        _context = context;
        _logger = logger;
        
        // Load API keys for different providers
        _providerApiKeys = new Dictionary<string, string>
        {
            ["FedEx"] = Environment.GetEnvironmentVariable("FEDEX_API_KEY") 
                       ?? configuration["Shipping:FedEx:ApiKey"] 
                       ?? string.Empty,
            ["UPS"] = Environment.GetEnvironmentVariable("UPS_API_KEY") 
                     ?? configuration["Shipping:UPS:ApiKey"] 
                     ?? string.Empty,
            ["USPS"] = Environment.GetEnvironmentVariable("USPS_API_KEY") 
                      ?? configuration["Shipping:USPS:ApiKey"] 
                      ?? string.Empty,
            ["DHL"] = Environment.GetEnvironmentVariable("DHL_API_KEY") 
                     ?? configuration["Shipping:DHL:ApiKey"] 
                     ?? string.Empty
        };

        if (_providerApiKeys.Values.All(string.IsNullOrEmpty))
        {
            _logger.LogWarning("No shipping provider API keys configured. Using mock mode.");
        }
    }

    public async Task<List<ShippingRate>> GetShippingRates(ShippingRateRequest request)
    {
        ValidateAddresses(request.FromAddress, request.ToAddress);
        ValidateDimensions(request.Dimensions);

        var rates = new List<ShippingRate>();

        // Get rates from all providers
        foreach (ShippingProvider provider in Enum.GetValues(typeof(ShippingProvider)))
        {
            foreach (ShippingSpeed speed in Enum.GetValues(typeof(ShippingSpeed)))
            {
                var cost = CalculateShippingCost(provider, speed, request.Dimensions.Weight);
                var estimatedDays = GetEstimatedDays(speed);

                rates.Add(new ShippingRate
                {
                    Provider = provider,
                    Speed = speed,
                    Cost = cost,
                    Currency = "USD",
                    EstimatedDays = estimatedDays,
                    EstimatedDelivery = DateTime.UtcNow.AddDays(estimatedDays),
                    ServiceName = $"{provider} {speed}"
                });
            }
        }

        return rates.OrderBy(r => r.Cost).ToList();
    }

    public async Task<ShipmentResponse> CreateShipment(CreateShipmentRequest request)
    {
        ValidateAddresses(request.FromAddress, request.ToAddress);
        ValidateDimensions(request.Dimensions);

        var cost = CalculateShippingCost(request.Provider, request.Speed, request.Dimensions.Weight);
        var estimatedDays = GetEstimatedDays(request.Speed);
        var trackingNumber = GenerateTrackingNumber(request.Provider);

        var shipment = new Shipment
        {
            TrackingNumber = trackingNumber,
            Provider = request.Provider,
            Speed = request.Speed,
            Status = ShipmentStatus.LabelGenerated,
            PaymentIntentId = request.PaymentIntentId,
            
            // From address
            FromName = request.FromAddress.Name,
            FromStreet1 = request.FromAddress.Street1,
            FromStreet2 = request.FromAddress.Street2,
            FromCity = request.FromAddress.City,
            FromState = request.FromAddress.State,
            FromPostalCode = request.FromAddress.PostalCode,
            FromCountry = request.FromAddress.Country,
            
            // To address
            ToName = request.ToAddress.Name,
            ToStreet1 = request.ToAddress.Street1,
            ToStreet2 = request.ToAddress.Street2,
            ToCity = request.ToAddress.City,
            ToState = request.ToAddress.State,
            ToPostalCode = request.ToAddress.PostalCode,
            ToCountry = request.ToAddress.Country,
            ToPhone = request.ToAddress.Phone,
            ToEmail = request.ToAddress.Email,
            
            // Package details
            Length = request.Dimensions.Length,
            Width = request.Dimensions.Width,
            Height = request.Dimensions.Height,
            Weight = request.Dimensions.Weight,
            
            ShippingCost = cost,
            EstimatedDelivery = DateTime.UtcNow.AddDays(estimatedDays),
            LabelUrl = GenerateMockLabelUrl(trackingNumber),
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Shipment created: {TrackingNumber} via {Provider}", 
            trackingNumber, request.Provider);

        return new ShipmentResponse
        {
            ShipmentId = shipment.Id,
            TrackingNumber = trackingNumber,
            Provider = request.Provider,
            Status = ShipmentStatus.LabelGenerated,
            ShippingCost = cost,
            LabelUrl = shipment.LabelUrl,
            EstimatedDelivery = shipment.EstimatedDelivery
        };
    }

    public async Task<Shipment?> GetShipment(int shipmentId)
    {
        return await _context.Shipments
            .Include(s => s.PaymentIntent)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);
    }

    public async Task<Shipment?> GetShipmentByTracking(string trackingNumber)
    {
        return await _context.Shipments
            .Include(s => s.PaymentIntent)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);
    }

    public async Task<List<TrackingUpdate>> GetTrackingUpdates(string trackingNumber)
    {
        var shipment = await GetShipmentByTracking(trackingNumber);
        
        if (shipment == null)
        {
            throw new ArgumentException($"Shipment with tracking number {trackingNumber} not found");
        }

        // Generate mock tracking updates based on shipment status
        return GenerateMockTrackingUpdates(shipment);
    }

    public async Task<Shipment> CancelShipment(int shipmentId)
    {
        var shipment = await GetShipment(shipmentId);
        
        if (shipment == null)
        {
            throw new ArgumentException($"Shipment {shipmentId} not found");
        }

        if (shipment.Status == ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot cancel a delivered shipment");
        }

        if (shipment.Status == ShipmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Shipment is already cancelled");
        }

        shipment.Status = ShipmentStatus.Cancelled;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Shipment cancelled: {TrackingNumber}", shipment.TrackingNumber);

        return shipment;
    }

    public async Task<List<Shipment>> GetShipmentHistory(string? customerEmail = null)
    {
        var query = _context.Shipments
            .Include(s => s.PaymentIntent)
            .AsQueryable();

        if (!string.IsNullOrEmpty(customerEmail))
        {
            query = query.Where(s => s.ToEmail == customerEmail);
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public decimal CalculateShippingCost(ShippingProvider provider, ShippingSpeed speed, decimal weight)
    {
        // Base rates per pound
        var baseRates = new Dictionary<ShippingProvider, decimal>
        {
            [ShippingProvider.USPS] = 5.00m,
            [ShippingProvider.FedEx] = 7.50m,
            [ShippingProvider.UPS] = 7.00m,
            [ShippingProvider.DHL] = 8.00m
        };

        // Speed multipliers
        var speedMultipliers = new Dictionary<ShippingSpeed, decimal>
        {
            [ShippingSpeed.Standard] = 1.0m,
            [ShippingSpeed.Express] = 1.5m,
            [ShippingSpeed.TwoDay] = 2.0m,
            [ShippingSpeed.Overnight] = 3.0m
        };

        var baseRate = baseRates[provider];
        var multiplier = speedMultipliers[speed];
        
        // Calculate: base rate + (weight * rate per pound) * speed multiplier
        var cost = (baseRate + (weight * 2.5m)) * multiplier;
        
        return Math.Round(cost, 2);
    }

    private void ValidateAddresses(ShippingAddress from, ShippingAddress to)
    {
        if (string.IsNullOrWhiteSpace(from.Street1) || string.IsNullOrWhiteSpace(from.City) ||
            string.IsNullOrWhiteSpace(from.State) || string.IsNullOrWhiteSpace(from.PostalCode))
        {
            throw new ArgumentException("From address is incomplete");
        }

        if (string.IsNullOrWhiteSpace(to.Street1) || string.IsNullOrWhiteSpace(to.City) ||
            string.IsNullOrWhiteSpace(to.State) || string.IsNullOrWhiteSpace(to.PostalCode))
        {
            throw new ArgumentException("To address is incomplete");
        }
    }

    private void ValidateDimensions(ShipmentDimensions dimensions)
    {
        if (dimensions.Length <= 0 || dimensions.Width <= 0 || 
            dimensions.Height <= 0 || dimensions.Weight <= 0)
        {
            throw new ArgumentException("Package dimensions must be greater than zero");
        }

        if (dimensions.Weight > 150)
        {
            throw new ArgumentException("Package weight cannot exceed 150 pounds");
        }
    }

    private int GetEstimatedDays(ShippingSpeed speed)
    {
        return speed switch
        {
            ShippingSpeed.Overnight => 1,
            ShippingSpeed.TwoDay => 2,
            ShippingSpeed.Express => 3,
            ShippingSpeed.Standard => 6,
            _ => 6
        };
    }

    private string GenerateTrackingNumber(ShippingProvider provider)
    {
        var prefix = provider switch
        {
            ShippingProvider.FedEx => "FX",
            ShippingProvider.UPS => "1Z",
            ShippingProvider.USPS => "94",
            ShippingProvider.DHL => "DH",
            _ => "XX"
        };

        var random = new Random();
        var number = random.Next(100000000, 999999999);
        
        return $"{prefix}{number:D9}";
    }

    private string GenerateMockLabelUrl(string trackingNumber)
    {
        return $"https://shipping-labels.example.com/{trackingNumber}.pdf";
    }

    private List<TrackingUpdate> GenerateMockTrackingUpdates(Shipment shipment)
    {
        var updates = new List<TrackingUpdate>();
        var currentDate = shipment.CreatedAt;

        updates.Add(new TrackingUpdate
        {
            TrackingNumber = shipment.TrackingNumber,
            Status = ShipmentStatus.LabelGenerated,
            Location = $"{shipment.FromCity}, {shipment.FromState}",
            Timestamp = currentDate,
            Description = "Shipping label created"
        });

        if (shipment.Status >= ShipmentStatus.PickedUp)
        {
            currentDate = currentDate.AddHours(4);
            updates.Add(new TrackingUpdate
            {
                TrackingNumber = shipment.TrackingNumber,
                Status = ShipmentStatus.PickedUp,
                Location = $"{shipment.FromCity}, {shipment.FromState}",
                Timestamp = currentDate,
                Description = "Package picked up"
            });
        }

        if (shipment.Status >= ShipmentStatus.InTransit)
        {
            currentDate = currentDate.AddDays(1);
            updates.Add(new TrackingUpdate
            {
                TrackingNumber = shipment.TrackingNumber,
                Status = ShipmentStatus.InTransit,
                Location = "Distribution Center",
                Timestamp = currentDate,
                Description = "In transit to destination"
            });
        }

        if (shipment.Status >= ShipmentStatus.OutForDelivery)
        {
            currentDate = currentDate.AddDays(1);
            updates.Add(new TrackingUpdate
            {
                TrackingNumber = shipment.TrackingNumber,
                Status = ShipmentStatus.OutForDelivery,
                Location = $"{shipment.ToCity}, {shipment.ToState}",
                Timestamp = currentDate,
                Description = "Out for delivery"
            });
        }

        if (shipment.Status == ShipmentStatus.Delivered)
        {
            currentDate = currentDate.AddHours(6);
            updates.Add(new TrackingUpdate
            {
                TrackingNumber = shipment.TrackingNumber,
                Status = ShipmentStatus.Delivered,
                Location = $"{shipment.ToStreet1}, {shipment.ToCity}, {shipment.ToState}",
                Timestamp = currentDate,
                Description = "Delivered"
            });
        }

        return updates;
    }
}
