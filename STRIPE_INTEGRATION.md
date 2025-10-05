# Stripe Payment Integration Guide

## 📋 Overview

This guide covers the complete Stripe payment integration for WindsurfProductAPI, including setup, usage, testing, and best practices.

## 🚀 Quick Start

### 1. Install Dependencies

The Stripe.net package is already included in the project:

```xml
<PackageReference Include="Stripe.net" Version="43.0.0" />
```

### 2. Configure Stripe API Keys

#### Option A: Environment Variables (Recommended for Production)

```bash
export STRIPE_SECRET_KEY="sk_test_your_secret_key_here"
export STRIPE_PUBLISHABLE_KEY="pk_test_your_publishable_key_here"
```

#### Option B: Configuration File (Development)

Add to `appsettings.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_secret_key_here",
    "PublishableKey": "pk_test_your_publishable_key_here"
  }
}
```

#### Option C: .env File

Create a `.env` file in the project root:

```env
STRIPE_SECRET_KEY=sk_test_your_secret_key_here
STRIPE_PUBLISHABLE_KEY=pk_test_your_publishable_key_here
```

### 3. Get Your Stripe Keys

1. Sign up at [stripe.com](https://stripe.com)
2. Go to **Developers** → **API keys**
3. Copy your **Publishable key** and **Secret key**
4. Use **test keys** (starting with `sk_test_` and `pk_test_`) for development

## 🏗️ Architecture

### Components

```
┌─────────────────────────────────────────────────────────┐
│                    Payment Flow                          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Client Request                                          │
│       ↓                                                  │
│  Payment Endpoints (Program.cs)                          │
│       ↓                                                  │
│  IPaymentService Interface                               │
│       ↓                                                  │
│  StripePaymentService Implementation                     │
│       ↓                                                  │
│  Stripe API / Mock Mode                                  │
│       ↓                                                  │
│  Database (PaymentIntent records)                        │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Key Files

- **Models**:
  - `PaymentIntent.cs` - Database entity for payment records
  - `PaymentRequest.cs` - Request DTOs
  - `PaymentResponse.cs` - Response DTOs
  - `PaymentConfirmation.cs` - Confirmation DTOs

- **Services**:
  - `IPaymentService.cs` - Service interface
  - `StripePaymentService.cs` - Stripe implementation

- **Endpoints**: `Program.cs` - Payment API endpoints

## 📡 API Endpoints

### 1. Create Payment Intent

**POST** `/api/payments/create`

Creates a new payment intent for purchasing a product.

**Request Body**:
```json
{
  "productId": 1,
  "quantity": 2,
  "customerEmail": "customer@example.com",
  "customerName": "John Doe",
  "currency": "usd"
}
```

**Response**:
```json
{
  "paymentIntentId": "pi_1234567890",
  "clientSecret": "pi_1234567890_secret_abcdef",
  "amount": 199.98,
  "currency": "usd",
  "status": "requires_payment_method",
  "publishableKey": "pk_test_..."
}
```

**cURL Example**:
```bash
curl -X POST http://localhost:5000/api/payments/create \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 1,
    "quantity": 2,
    "customerEmail": "customer@example.com",
    "customerName": "John Doe"
  }'
```

### 2. Confirm Payment

**POST** `/api/payments/{paymentIntentId}/confirm`

Confirms a payment and retrieves its current status.

**Response**:
```json
{
  "paymentIntentId": "pi_1234567890",
  "status": "succeeded",
  "amount": 199.98,
  "receiptUrl": "https://pay.stripe.com/receipts/..."
}
```

**cURL Example**:
```bash
curl -X POST http://localhost:5000/api/payments/pi_1234567890/confirm
```

### 3. Cancel Payment

**POST** `/api/payments/{paymentIntentId}/cancel`

Cancels a pending payment intent.

**Response**:
```json
{
  "paymentIntentId": "pi_1234567890",
  "status": "canceled",
  "amount": 199.98
}
```

### 4. Get Payment Status

**GET** `/api/payments/{paymentIntentId}`

Retrieves the current status of a payment.

**Response**:
```json
{
  "id": 1,
  "stripePaymentIntentId": "pi_1234567890",
  "productId": 1,
  "product": {
    "id": 1,
    "name": "Wireless Headphones",
    "price": 99.99
  },
  "amount": 199.98,
  "currency": "usd",
  "status": "succeeded",
  "customerEmail": "customer@example.com",
  "customerName": "John Doe",
  "createdAt": "2025-10-05T14:30:00Z",
  "completedAt": "2025-10-05T14:31:00Z"
}
```

### 5. Get Payment History

**GET** `/api/payments?customerEmail={email}`

Retrieves payment history, optionally filtered by customer email.

**Response**:
```json
[
  {
    "id": 1,
    "stripePaymentIntentId": "pi_1234567890",
    "productId": 1,
    "amount": 199.98,
    "status": "succeeded",
    "customerEmail": "customer@example.com",
    "createdAt": "2025-10-05T14:30:00Z"
  }
]
```

### 6. Calculate Price

**GET** `/api/products/{productId}/calculate-price?quantity={quantity}`

Calculates the total price before creating a payment.

**Response**:
```json
{
  "productId": 1,
  "quantity": 2,
  "totalAmount": 199.98
}
```

## 💻 Usage Examples

### Frontend Integration (JavaScript)

```javascript
// 1. Create payment intent
async function createPayment(productId, quantity) {
  const response = await fetch('/api/payments/create', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      productId: productId,
      quantity: quantity,
      customerEmail: 'customer@example.com',
      customerName: 'John Doe'
    })
  });
  
  const payment = await response.json();
  return payment;
}

// 2. Use Stripe.js to handle payment on frontend
const stripe = Stripe(payment.publishableKey);
const { error } = await stripe.confirmCardPayment(payment.clientSecret, {
  payment_method: {
    card: cardElement,
    billing_details: {
      name: 'John Doe',
      email: 'customer@example.com'
    }
  }
});

// 3. Confirm payment on backend
if (!error) {
  const confirmation = await fetch(
    `/api/payments/${payment.paymentIntentId}/confirm`,
    { method: 'POST' }
  );
  const result = await confirmation.json();
  console.log('Payment succeeded:', result);
}
```

### C# Client Example

```csharp
using System.Net.Http.Json;

public class PaymentClient
{
    private readonly HttpClient _httpClient;

    public PaymentClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentResponse> CreatePaymentAsync(
        int productId, 
        int quantity, 
        string email)
    {
        var request = new PaymentRequest
        {
            ProductId = productId,
            Quantity = quantity,
            CustomerEmail = email
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/payments/create", 
            request
        );
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResponse>();
    }

    public async Task<PaymentConfirmation> ConfirmPaymentAsync(
        string paymentIntentId)
    {
        var response = await _httpClient.PostAsync(
            $"/api/payments/{paymentIntentId}/confirm", 
            null
        );
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentConfirmation>();
    }
}
```

## 🧪 Testing

### Mock Mode (No API Key Required)

The service automatically runs in **mock mode** when no Stripe API key is configured. This is perfect for:
- Development without Stripe account
- Running automated tests
- CI/CD pipelines

In mock mode:
- Payment intents are created with mock IDs
- All operations work as expected
- No actual charges are made
- Data is stored in the database

### Unit Tests

Run unit tests for the payment service:

```bash
cd WindsurfProductAPI.Tests
dotnet test --filter "FullyQualifiedName~PaymentServiceTests"
```

**Example Test**:
```csharp
[Fact]
public async Task CreatePaymentIntent_WithValidProduct_ShouldCreatePayment()
{
    // Arrange
    var request = new PaymentRequest
    {
        ProductId = 1,
        Quantity = 2,
        CustomerEmail = "test@example.com"
    };

    // Act
    var response = await _paymentService.CreatePaymentIntent(request);

    // Assert
    response.Should().NotBeNull();
    response.Amount.Should().Be(200m);
}
```

### Integration Tests

Run integration tests for payment endpoints:

```bash
dotnet test --filter "FullyQualifiedName~PaymentApiTests"
```

### BDD Tests (SpecFlow)

Run BDD scenarios:

```bash
dotnet test --filter "FullyQualifiedName~PaymentProcessingSteps"
```

**Example Scenario**:
```gherkin
Scenario: Create a payment intent for a single product
    When I create a payment for product 1 with quantity 1
    Then the payment should be created successfully
    And the payment amount should be 29.99
```

## 🔒 Security Best Practices

### 1. Never Expose Secret Keys

❌ **DON'T**:
```csharp
var secretKey = "sk_live_abc123"; // Hardcoded!
```

✅ **DO**:
```csharp
var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
```

### 2. Use Environment-Specific Keys

- **Development**: Use test keys (`sk_test_...`)
- **Production**: Use live keys (`sk_live_...`)
- Store keys in secure vaults (Azure Key Vault, AWS Secrets Manager)

### 3. Validate on Server Side

Always validate payments on the server, never trust client-side confirmation alone.

### 4. Use HTTPS

Ensure all payment endpoints are served over HTTPS in production.

### 5. Implement Webhooks

For production, implement Stripe webhooks to handle:
- Payment success/failure notifications
- Refunds
- Disputes

## 📊 Database Schema

### PaymentIntent Table

```sql
CREATE TABLE PaymentIntents (
    Id INT PRIMARY KEY,
    StripePaymentIntentId VARCHAR(255),
    ProductId INT FOREIGN KEY,
    Amount DECIMAL(18,2),
    Currency VARCHAR(3),
    Status VARCHAR(50),
    CustomerEmail VARCHAR(255),
    CustomerName VARCHAR(255),
    CreatedAt DATETIME,
    CompletedAt DATETIME NULL,
    FailureReason VARCHAR(500) NULL
);
```

## 🔄 Payment Workflow

### Standard Flow

```
1. Customer selects product
   ↓
2. Frontend calls /api/payments/create
   ↓
3. Backend creates Stripe Payment Intent
   ↓
4. Frontend receives clientSecret
   ↓
5. Customer enters card details (Stripe.js)
   ↓
6. Stripe processes payment
   ↓
7. Frontend calls /api/payments/{id}/confirm
   ↓
8. Backend verifies with Stripe
   ↓
9. Payment complete!
```

### Error Handling

```csharp
try
{
    var payment = await paymentService.CreatePaymentIntent(request);
    // Handle success
}
catch (ArgumentException ex)
{
    // Invalid product or quantity
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    // Stripe API error or other issues
    return StatusCode(500, ex.Message);
}
```

## 🎯 Common Scenarios

### Scenario 1: Simple Product Purchase

```bash
# 1. Calculate price
curl http://localhost:5000/api/products/1/calculate-price?quantity=2

# 2. Create payment
curl -X POST http://localhost:5000/api/payments/create \
  -H "Content-Type: application/json" \
  -d '{"productId":1,"quantity":2,"customerEmail":"buyer@example.com"}'

# 3. Confirm payment (after Stripe.js processing)
curl -X POST http://localhost:5000/api/payments/pi_xxx/confirm
```

### Scenario 2: View Customer History

```bash
curl http://localhost:5000/api/payments?customerEmail=buyer@example.com
```

### Scenario 3: Cancel Pending Payment

```bash
curl -X POST http://localhost:5000/api/payments/pi_xxx/cancel
```

## 🐛 Troubleshooting

### Issue: "Stripe API key not configured"

**Solution**: Set the `STRIPE_SECRET_KEY` environment variable or add it to `appsettings.json`.

### Issue: "Product not found"

**Solution**: Ensure the product exists in the database before creating a payment.

### Issue: "Payment intent not found"

**Solution**: Verify the payment intent ID is correct and exists in your database.

### Issue: Tests failing with Stripe errors

**Solution**: Tests run in mock mode by default. Ensure no API key is set during testing.

## 📚 Additional Resources

- [Stripe API Documentation](https://stripe.com/docs/api)
- [Stripe.net Library](https://github.com/stripe/stripe-dotnet)
- [Stripe Testing Cards](https://stripe.com/docs/testing)
- [Payment Intents Guide](https://stripe.com/docs/payments/payment-intents)

## 🎓 Next Steps

1. **Set up webhooks** for production payment notifications
2. **Implement refunds** functionality
3. **Add subscription support** for recurring payments
4. **Integrate with order management** system
5. **Add payment analytics** and reporting

## 💡 Tips

- Use Stripe's test card `4242 4242 4242 4242` for testing
- Monitor payments in the Stripe Dashboard
- Enable Stripe Radar for fraud prevention
- Use Stripe Checkout for a hosted payment page option
- Implement idempotency keys for safe retries

---

**Need Help?** Check the test files for working examples or refer to the Stripe documentation.
