# Shipping Provider Integration Guide

## 📋 Overview

Complete multi-carrier shipping integration supporting FedEx, UPS, USPS, and DHL with rate comparison, label generation, and tracking.

## 🚀 Quick Start

### Supported Carriers
- **FedEx** - Express and ground shipping
- **UPS** - Worldwide delivery
- **USPS** - Domestic and international
- **DHL** - International express

### Features
✅ Multi-carrier rate comparison  
✅ Automatic label generation  
✅ Real-time tracking  
✅ Shipment history  
✅ Cost calculation  
✅ Mock mode (no API keys required)

## 📡 API Endpoints

### 1. Get Shipping Rates
**POST** `/api/shipping/rates`

Compare rates from all carriers.

**Request:**
```json
{
  "fromAddress": {
    "name": "Warehouse",
    "street1": "123 Main St",
    "city": "San Francisco",
    "state": "CA",
    "postalCode": "94102",
    "country": "US"
  },
  "toAddress": {
    "name": "Customer",
    "street1": "456 Oak Ave",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001",
    "country": "US"
  },
  "dimensions": {
    "length": 12,
    "width": 10,
    "height": 8,
    "weight": 5
  }
}
```

**Response:**
```json
[
  {
    "provider": "USPS",
    "speed": "Standard",
    "cost": 17.50,
    "currency": "USD",
    "estimatedDays": 6,
    "estimatedDelivery": "2025-10-11T00:00:00Z",
    "serviceName": "USPS Standard"
  },
  {
    "provider": "FedEx",
    "speed": "Express",
    "cost": 32.50,
    "currency": "USD",
    "estimatedDays": 3,
    "estimatedDelivery": "2025-10-08T00:00:00Z",
    "serviceName": "FedEx Express"
  }
]
```

### 2. Create Shipment
**POST** `/api/shipping/create`

**Request:**
```json
{
  "provider": "FedEx",
  "speed": "Express",
  "fromAddress": { /* same as above */ },
  "toAddress": { /* same as above */ },
  "dimensions": { /* same as above */ },
  "paymentIntentId": 123,
  "notes": "Handle with care"
}
```

**Response:**
```json
{
  "shipmentId": 1,
  "trackingNumber": "FX123456789",
  "provider": "FedEx",
  "status": "LabelGenerated",
  "shippingCost": 32.50,
  "labelUrl": "https://shipping-labels.example.com/FX123456789.pdf",
  "estimatedDelivery": "2025-10-08T00:00:00Z"
}
```

### 3. Track Shipment
**GET** `/api/shipping/track/{trackingNumber}`

### 4. Get Tracking Updates
**GET** `/api/shipping/track/{trackingNumber}/updates`

**Response:**
```json
[
  {
    "trackingNumber": "FX123456789",
    "status": "LabelGenerated",
    "location": "San Francisco, CA",
    "timestamp": "2025-10-05T10:00:00Z",
    "description": "Shipping label created"
  },
  {
    "trackingNumber": "FX123456789",
    "status": "PickedUp",
    "location": "San Francisco, CA",
    "timestamp": "2025-10-05T14:00:00Z",
    "description": "Package picked up"
  }
]
```

### 5. Cancel Shipment
**POST** `/api/shipping/{shipmentId}/cancel`

### 6. Get Shipment History
**GET** `/api/shipping?customerEmail={email}`

### 7. Calculate Shipping Cost
**GET** `/api/shipping/calculate-cost?provider=FedEx&speed=Express&weight=5`

## 💻 Usage Examples

### JavaScript/TypeScript
```javascript
// Get shipping rates
const rates = await fetch('/api/shipping/rates', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    fromAddress: { /* ... */ },
    toAddress: { /* ... */ },
    dimensions: { length: 12, width: 10, height: 8, weight: 5 }
  })
});

// Create shipment
const shipment = await fetch('/api/shipping/create', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    provider: 'FedEx',
    speed: 'Express',
    fromAddress: { /* ... */ },
    toAddress: { /* ... */ },
    dimensions: { /* ... */ }
  })
});

// Track shipment
const tracking = await fetch(`/api/shipping/track/${trackingNumber}`);
```

### C#
```csharp
var rateRequest = new ShippingRateRequest
{
    FromAddress = new ShippingAddress { /* ... */ },
    ToAddress = new ShippingAddress { /* ... */ },
    Dimensions = new ShipmentDimensions { Weight = 5 }
};

var rates = await shippingService.GetShippingRates(rateRequest);
var cheapest = rates.OrderBy(r => r.Cost).First();

var shipment = await shippingService.CreateShipment(new CreateShipmentRequest
{
    Provider = cheapest.Provider,
    Speed = cheapest.Speed,
    FromAddress = rateRequest.FromAddress,
    ToAddress = rateRequest.ToAddress,
    Dimensions = rateRequest.Dimensions
});
```

## 🧪 Testing

### Run All Shipping Tests
```bash
cd WindsurfProductAPI.Tests
dotnet test --filter "Shipping"
```

### Test Categories
```bash
# Unit tests
dotnet test --filter "ShippingServiceTests"

# Integration tests
dotnet test --filter "ShippingApiTests"

# BDD tests
dotnet test --filter "ShippingManagementSteps"
```

## 📊 Shipping Rates

### Base Rates (per pound)
| Provider | Standard | Express | TwoDay | Overnight |
|----------|----------|---------|--------|-----------|
| USPS     | $5.00    | $7.50   | $10.00 | $15.00    |
| FedEx    | $7.50    | $11.25  | $15.00 | $22.50    |
| UPS      | $7.00    | $10.50  | $14.00 | $21.00    |
| DHL      | $8.00    | $12.00  | $16.00 | $24.00    |

### Calculation Formula
```
Cost = (BaseRate + (Weight × $2.50)) × SpeedMultiplier
```

## 🔧 Configuration

### Environment Variables
```bash
export FEDEX_API_KEY="your_fedex_key"
export UPS_API_KEY="your_ups_key"
export USPS_API_KEY="your_usps_key"
export DHL_API_KEY="your_dhl_key"
```

### appsettings.json
```json
{
  "Shipping": {
    "FedEx": { "ApiKey": "your_key" },
    "UPS": { "ApiKey": "your_key" },
    "USPS": { "ApiKey": "your_key" },
    "DHL": { "ApiKey": "your_key" }
  }
}
```

## 📦 Complete Workflow

```bash
# 1. Get rates
curl -X POST http://localhost:5000/api/shipping/rates \
  -H "Content-Type: application/json" \
  -d '{"fromAddress":{...},"toAddress":{...},"dimensions":{...}}'

# 2. Create shipment
curl -X POST http://localhost:5000/api/shipping/create \
  -H "Content-Type: application/json" \
  -d '{"provider":"FedEx","speed":"Express",...}'

# 3. Track shipment
curl http://localhost:5000/api/shipping/track/FX123456789

# 4. Get tracking updates
curl http://localhost:5000/api/shipping/track/FX123456789/updates

# 5. Cancel if needed
curl -X POST http://localhost:5000/api/shipping/1/cancel
```

## 🎯 Best Practices

1. **Always get rates first** - Compare before creating shipment
2. **Validate addresses** - Ensure complete address information
3. **Store tracking numbers** - Link to orders/payments
4. **Handle errors gracefully** - Carrier APIs can fail
5. **Use webhooks** - For real-time tracking updates (production)

## 📚 Additional Resources

- [FedEx Developer Portal](https://developer.fedex.com)
- [UPS Developer Kit](https://www.ups.com/upsdeveloperkit)
- [USPS Web Tools](https://www.usps.com/business/web-tools-apis/)
- [DHL Developer Portal](https://developer.dhl.com)

---

**Mock Mode**: Works without API keys for testing!
