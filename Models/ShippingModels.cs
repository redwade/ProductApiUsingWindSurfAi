namespace WindsurfProductAPI.Models;

public enum ShippingProvider
{
    FedEx,
    UPS,
    USPS,
    DHL
}

public enum ShippingSpeed
{
    Standard,      // 5-7 business days
    Express,       // 2-3 business days
    Overnight,     // Next business day
    TwoDay         // 2 business days
}

public enum ShipmentStatus
{
    Created,
    LabelGenerated,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    Failed,
    Cancelled
}

public class ShippingAddress
{
    public string Name { get; set; } = string.Empty;
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class ShipmentDimensions
{
    public decimal Length { get; set; }  // inches
    public decimal Width { get; set; }   // inches
    public decimal Height { get; set; }  // inches
    public decimal Weight { get; set; }  // pounds
}

public class Shipment
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public ShippingProvider Provider { get; set; }
    public ShippingSpeed Speed { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
    
    public int? OrderId { get; set; }
    public int? PaymentIntentId { get; set; }
    public PaymentIntent? PaymentIntent { get; set; }
    
    // Addresses
    public string FromName { get; set; } = string.Empty;
    public string FromStreet1 { get; set; } = string.Empty;
    public string? FromStreet2 { get; set; }
    public string FromCity { get; set; } = string.Empty;
    public string FromState { get; set; } = string.Empty;
    public string FromPostalCode { get; set; } = string.Empty;
    public string FromCountry { get; set; } = "US";
    
    public string ToName { get; set; } = string.Empty;
    public string ToStreet1 { get; set; } = string.Empty;
    public string? ToStreet2 { get; set; }
    public string ToCity { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public string ToPostalCode { get; set; } = string.Empty;
    public string ToCountry { get; set; } = "US";
    public string? ToPhone { get; set; }
    public string? ToEmail { get; set; }
    
    // Package details
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    
    // Costs
    public decimal ShippingCost { get; set; }
    public string Currency { get; set; } = "USD";
    
    // Tracking
    public string? LabelUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? ActualDelivery { get; set; }
    public string? Notes { get; set; }
}

public class ShippingRateRequest
{
    public ShippingAddress FromAddress { get; set; } = new();
    public ShippingAddress ToAddress { get; set; } = new();
    public ShipmentDimensions Dimensions { get; set; } = new();
}

public class ShippingRate
{
    public ShippingProvider Provider { get; set; }
    public ShippingSpeed Speed { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = "USD";
    public int EstimatedDays { get; set; }
    public DateTime EstimatedDelivery { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}

public class CreateShipmentRequest
{
    public ShippingProvider Provider { get; set; }
    public ShippingSpeed Speed { get; set; }
    public ShippingAddress FromAddress { get; set; } = new();
    public ShippingAddress ToAddress { get; set; } = new();
    public ShipmentDimensions Dimensions { get; set; } = new();
    public int? PaymentIntentId { get; set; }
    public string? Notes { get; set; }
}

public class ShipmentResponse
{
    public int ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public ShippingProvider Provider { get; set; }
    public ShipmentStatus Status { get; set; }
    public decimal ShippingCost { get; set; }
    public string? LabelUrl { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
}

public class TrackingUpdate
{
    public string TrackingNumber { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Description { get; set; }
}
