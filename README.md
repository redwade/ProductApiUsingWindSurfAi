# 🚀 Windsurf Product API

A powerful .NET 8 Web API with **Windsurf AI Integration** for intelligent product catalog management. This API demonstrates cutting-edge AI capabilities including automated marketing content generation, product positioning analysis, pricing insights, and catalog-wide business intelligence.

## ✨ Features

### Core Functionality
- **RESTful API** with Minimal API architecture
- **Entity Framework Core** with In-Memory Database
- **Swagger/OpenAPI** documentation
- **CRUD Operations** for product management

### 🤖 AI-Powered Features
- **Marketing Description Generation** - Create compelling product descriptions automatically
- **Product Positioning Analysis** - Get insights on target market and competitive positioning
- **Pricing Strategy Analysis** - Receive AI-powered pricing recommendations
- **Automatic Categorization** - Intelligently categorize products
- **Catalog-Wide Insights** - Generate actionable business intelligence from your entire catalog
- **Batch Processing** - Analyze multiple products simultaneously

### 💳 Payment Integration
- **Stripe Payment Processing** - Secure payment handling with Stripe
- **Payment Intent Management** - Create, confirm, and cancel payments
- **Payment History** - Track all transactions
- **Price Calculation** - Calculate totals before payment
- **Mock Mode** - Test without Stripe API keys

## 🛠️ Tech Stack

- **.NET 8.0**
- **Entity Framework Core 8.0** (In-Memory Database)
- **Swagger/Swashbuckle** for API documentation
- **Windsurf AI API** integration
- **Minimal API** architecture

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windsurf AI API Key (optional - works with mock data if not provided)

## 🚀 Getting Started

### 1. Clone or Navigate to the Project

```bash
cd /Users/tominjose/CascadeProjects/WindsurfProductAPI
```

### 2. Configure Windsurf AI API Key

You have two options:

#### Option A: Environment Variable (Recommended)
```bash
export WINDSURF_API_KEY="your_windsurf_api_key_here"
```

Add to `~/.zshrc` for persistence:
```bash
echo 'export WINDSURF_API_KEY="your_windsurf_api_key_here"' >> ~/.zshrc
source ~/.zshrc
```

#### Option B: Configuration File
Edit `appsettings.json`:
```json
{
  "WindsurfAI": {
    "ApiKey": "your_windsurf_api_key_here"
  }
}
```

> **Note**: The API works with mock AI data if no API key is configured, perfect for testing!

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Run the Application

```bash
dotnet run
```

The API will start at:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `http://localhost:5000` or `https://localhost:5001`

## 📚 API Endpoints

### Product Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create new product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Delete product |

### 🤖 AI-Powered Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/products/{id}/ai-insights` | Generate complete AI insights |
| POST | `/api/products/{id}/marketing-description` | Generate marketing description |
| POST | `/api/products/{id}/positioning` | Analyze product positioning |
| POST | `/api/products/{id}/pricing-analysis` | Analyze pricing strategy |
| POST | `/api/products/{id}/suggest-category` | Suggest optimal category |
| POST | `/api/catalog/ai-insights` | Generate catalog-wide insights |
| POST | `/api/catalog/batch-analyze` | Batch analyze all products |

### System

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check and status |

## 💡 Usage Examples

### Create a Product

```bash
curl -X POST https://localhost:5001/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Premium Laptop",
    "description": "High-performance laptop for professionals",
    "price": 1299.99,
    "category": "Electronics"
  }'
```

### Generate AI Insights

```bash
curl -X POST https://localhost:5001/api/products/1/ai-insights
```

### Get Catalog Insights

```bash
curl -X POST https://localhost:5001/api/catalog/ai-insights
```

### Batch Analyze Catalog

```bash
curl -X POST https://localhost:5001/api/catalog/batch-analyze
```

## 🎯 Sample Data

The API comes pre-seeded with sample products:

1. **Wireless Headphones** - $199.99 (Electronics)
2. **Smart Watch** - $299.99 (Electronics)
3. **Yoga Mat** - $49.99 (Sports)

## 🏗️ Project Structure

```
WindsurfProductAPI/
├── Models/
│   ├── Product.cs                  # Product entity
│   ├── ProductCreateDto.cs         # DTO for creating products
│   ├── AIInsightResponse.cs        # AI insights response model
│   └── CatalogInsights.cs          # Catalog analysis model
├── Data/
│   └── ProductDbContext.cs         # EF Core DbContext
├── Services/
│   ├── IWindsurfAIService.cs       # AI service interface
│   └── WindsurfAIService.cs        # AI service implementation
├── Program.cs                       # Application entry point & API endpoints
├── appsettings.json                 # Configuration
└── WindsurfProductAPI.csproj        # Project file
```

## 🔧 Configuration

### appsettings.json

```json
{
  "WindsurfAI": {
    "ApiKey": "",
    "BaseUrl": "https://api.windsurf.ai/v1"
  }
}
```

### Environment Variables

- `WINDSURF_API_KEY` - Your Windsurf AI API key (takes precedence over config file)

## 🧪 Testing with Swagger

1. Navigate to `https://localhost:5001` in your browser
2. Explore all available endpoints
3. Try out the AI-powered features:
   - Create a product
   - Generate AI insights
   - Analyze your catalog

## 🎨 AI Capabilities Showcase

### Marketing Description Generation
Transform basic product info into compelling marketing copy:
```
Input: "Wireless Headphones - High-quality wireless headphones"
Output: "✨ Discover exceptional audio quality with our premium wireless headphones..."
```

### Product Positioning Analysis
Get strategic insights:
- Target market identification
- Competitive positioning
- Unique value proposition
- Key differentiators

### Pricing Analysis
Receive actionable recommendations:
- Price competitiveness assessment
- Value perception analysis
- Market fit evaluation
- Pricing strategy suggestions

### Catalog Intelligence
Understand your entire product portfolio:
- Category distribution
- Price range analysis
- Product mix recommendations
- Growth opportunities

## 🔒 Security Best Practices

1. **Never commit API keys** to version control
2. Use **environment variables** for sensitive data
3. Keep `.env` files in `.gitignore`
4. Rotate API keys regularly
5. Use HTTPS in production

## 🚀 Deployment

### Build for Production

```bash
dotnet publish -c Release -o ./publish
```

### Run in Production

```bash
cd publish
WINDSURF_API_KEY="your_key" dotnet WindsurfProductAPI.dll
```

## 📊 Performance

- **In-Memory Database** for fast development and testing
- **Async/await** throughout for optimal performance
- **HTTP Client pooling** for efficient API calls
- **Minimal API** for reduced overhead

## 🤝 Contributing

Feel free to submit issues and enhancement requests!

## 📄 License

This project is provided as-is for demonstration purposes.

## 🎓 Learning Resources

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet)
- [Minimal APIs](https://docs.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Swagger/OpenAPI](https://swagger.io)

## 🌟 What Makes This Special?

This API showcases how AI can transform traditional CRUD operations into intelligent business tools:

- **Automated Content Creation** - Generate marketing materials instantly
- **Strategic Insights** - Make data-driven business decisions
- **Time Savings** - Automate repetitive analysis tasks
- **Scalability** - Process entire catalogs in seconds
- **Modern Architecture** - Built with .NET 8 best practices

---

**Built with ❤️ using .NET 8 and Windsurf AI**
