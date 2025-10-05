using System.Text;
using System.Text.Json;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Services;

public class WindsurfAIService : IWindsurfAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WindsurfAIService> _logger;
    private readonly string _apiKey;

    public WindsurfAIService(HttpClient httpClient, IConfiguration configuration, ILogger<WindsurfAIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Try to get API key from environment variable first, then configuration
        _apiKey = Environment.GetEnvironmentVariable("WINDSURF_API_KEY") 
                  ?? configuration["WindsurfAI:ApiKey"] 
                  ?? string.Empty;

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Windsurf API key not configured. AI features will use mock data.");
        }
    }

    public async Task<AIInsightResponse> GenerateProductInsights(Product product)
    {
        var insights = new AIInsightResponse
        {
            ProductId = product.Id,
            ProductName = product.Name
        };

        try
        {
            insights.MarketingDescription = await GenerateMarketingDescription(product);
            insights.Positioning = await AnalyzeProductPositioning(product);
            insights.PricingAnalysis = await AnalyzePricing(product);
            insights.SuggestedCategory = await SuggestCategory(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI insights for product {ProductId}", product.Id);
            throw;
        }

        return insights;
    }

    public async Task<string> GenerateMarketingDescription(Product product)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return GenerateMockMarketingDescription(product);
        }

        var prompt = $@"Generate a compelling marketing description for the following product:
Name: {product.Name}
Description: {product.Description}
Price: ${product.Price}
Category: {product.Category}

Create an engaging, persuasive description that highlights key benefits and appeals to target customers.";

        return await CallWindsurfAI(prompt);
    }

    public async Task<string> AnalyzeProductPositioning(Product product)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return GenerateMockPositioning(product);
        }

        var prompt = $@"Analyze the market positioning for this product:
Name: {product.Name}
Description: {product.Description}
Price: ${product.Price}
Category: {product.Category}

Provide insights on target market, competitive positioning, and unique value proposition.";

        return await CallWindsurfAI(prompt);
    }

    public async Task<string> AnalyzePricing(Product product)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return GenerateMockPricingAnalysis(product);
        }

        var prompt = $@"Analyze the pricing strategy for this product:
Name: {product.Name}
Description: {product.Description}
Price: ${product.Price}
Category: {product.Category}

Provide insights on pricing competitiveness, perceived value, and recommendations.";

        return await CallWindsurfAI(prompt);
    }

    public async Task<string> SuggestCategory(Product product)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return GenerateMockCategory(product);
        }

        var prompt = $@"Suggest the most appropriate product category for:
Name: {product.Name}
Description: {product.Description}
Current Category: {product.Category}

Provide a single, specific category name that best fits this product.";

        return await CallWindsurfAI(prompt);
    }

    public async Task<CatalogInsights> GenerateCatalogInsights(IEnumerable<Product> products)
    {
        var productList = products.ToList();
        
        var insights = new CatalogInsights
        {
            TotalProducts = productList.Count,
            CategoryDistribution = productList.GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            AveragePrice = productList.Any() ? productList.Average(p => p.Price) : 0,
            MinPrice = productList.Any() ? productList.Min(p => p.Price) : 0,
            MaxPrice = productList.Any() ? productList.Max(p => p.Price) : 0
        };

        if (string.IsNullOrEmpty(_apiKey))
        {
            insights.AIRecommendations = GenerateMockCatalogRecommendations(insights);
            return insights;
        }

        var prompt = $@"Analyze this product catalog and provide business insights:
Total Products: {insights.TotalProducts}
Categories: {string.Join(", ", insights.CategoryDistribution.Select(c => $"{c.Key} ({c.Value})"))}
Price Range: ${insights.MinPrice} - ${insights.MaxPrice}
Average Price: ${insights.AveragePrice:F2}

Provide actionable recommendations for catalog optimization, pricing strategy, and product mix.";

        insights.AIRecommendations = await CallWindsurfAI(prompt);
        return insights;
    }

    private async Task<string> CallWindsurfAI(string prompt)
    {
        try
        {
            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "You are a product marketing and business analysis expert." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync("/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseBody);
            
            return jsonDoc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No response generated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Windsurf AI API");
            throw new Exception("Failed to generate AI insights. Please check your API key and try again.", ex);
        }
    }

    // Mock data generators for when API key is not configured
    private string GenerateMockMarketingDescription(Product product)
    {
        return $"✨ Discover the exceptional {product.Name}! {product.Description} " +
               $"At just ${product.Price}, this premium {product.Category.ToLower()} product offers " +
               $"unmatched value and quality. Perfect for discerning customers who demand the best. " +
               $"Don't miss out on this opportunity to elevate your experience!";
    }

    private string GenerateMockPositioning(Product product)
    {
        var pricePoint = product.Price switch
        {
            < 50 => "budget-friendly",
            < 200 => "mid-range",
            _ => "premium"
        };

        return $"**Target Market**: {product.Category} enthusiasts seeking {pricePoint} solutions\n" +
               $"**Competitive Position**: {pricePoint.ToUpper()} segment with strong value proposition\n" +
               $"**Unique Value**: Quality and reliability at ${product.Price}\n" +
               $"**Key Differentiator**: {product.Description}";
    }

    private string GenerateMockPricingAnalysis(Product product)
    {
        var assessment = product.Price switch
        {
            < 50 => "highly competitive and accessible",
            < 200 => "well-positioned in the mid-market",
            _ => "premium pricing reflecting quality"
        };

        return $"**Price Point**: ${product.Price} is {assessment}\n" +
               $"**Value Perception**: Strong value for money in the {product.Category} category\n" +
               $"**Recommendation**: Current pricing aligns well with product positioning\n" +
               $"**Market Fit**: Attractive to target demographic";
    }

    private string GenerateMockCategory(Product product)
    {
        // Simple category suggestion based on keywords
        var name = product.Name.ToLower();
        var desc = product.Description.ToLower();
        
        if (name.Contains("watch") || name.Contains("headphone") || name.Contains("phone"))
            return "Electronics";
        if (name.Contains("yoga") || name.Contains("fitness") || desc.Contains("sport"))
            return "Sports & Fitness";
        if (name.Contains("book") || desc.Contains("read"))
            return "Books & Media";
        
        return product.Category; // Return current category if no match
    }

    private string GenerateMockCatalogRecommendations(CatalogInsights insights)
    {
        var recommendations = new StringBuilder();
        recommendations.AppendLine("📊 **Catalog Analysis & Recommendations**\n");
        
        recommendations.AppendLine($"**Portfolio Overview**: Your catalog contains {insights.TotalProducts} products " +
                                 $"across {insights.CategoryDistribution.Count} categories.\n");
        
        recommendations.AppendLine($"**Pricing Strategy**: Price range of ${insights.MinPrice:F2} - ${insights.MaxPrice:F2} " +
                                 $"with an average of ${insights.AveragePrice:F2} indicates a {GetPriceRangeAssessment(insights)}.\n");
        
        recommendations.AppendLine("**Key Recommendations**:");
        recommendations.AppendLine("• Consider expanding underrepresented categories");
        recommendations.AppendLine("• Optimize product descriptions for better conversion");
        recommendations.AppendLine("• Implement dynamic pricing for competitive products");
        recommendations.AppendLine("• Focus marketing on high-margin items");
        recommendations.AppendLine("• Bundle complementary products for increased AOV");
        
        return recommendations.ToString();
    }

    private string GetPriceRangeAssessment(CatalogInsights insights)
    {
        var range = insights.MaxPrice - insights.MinPrice;
        if (range < 100) return "focused price segment";
        if (range < 500) return "diverse price range";
        return "wide price spectrum serving multiple market segments";
    }
}
