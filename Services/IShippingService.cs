using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Services;

public interface IShippingService
{
    Task<List<ShippingRate>> GetShippingRates(ShippingRateRequest request);
    Task<ShipmentResponse> CreateShipment(CreateShipmentRequest request);
    Task<Shipment?> GetShipment(int shipmentId);
    Task<Shipment?> GetShipmentByTracking(string trackingNumber);
    Task<List<TrackingUpdate>> GetTrackingUpdates(string trackingNumber);
    Task<Shipment> CancelShipment(int shipmentId);
    Task<List<Shipment>> GetShipmentHistory(string? customerEmail = null);
    decimal CalculateShippingCost(ShippingProvider provider, ShippingSpeed speed, decimal weight);
}
