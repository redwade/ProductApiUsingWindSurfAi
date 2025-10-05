using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Services;

public interface IWindsurfAIService
{
    Task<AIInsightResponse> GenerateProductInsights(Product product);
    Task<string> GenerateMarketingDescription(Product product);
    Task<string> AnalyzeProductPositioning(Product product);
    Task<string> AnalyzePricing(Product product);
    Task<string> SuggestCategory(Product product);
    Task<CatalogInsights> GenerateCatalogInsights(IEnumerable<Product> products);
}
