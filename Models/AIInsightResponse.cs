namespace WindsurfProductAPI.Models;

public class AIInsightResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string MarketingDescription { get; set; } = string.Empty;
    public string Positioning { get; set; } = string.Empty;
    public string PricingAnalysis { get; set; } = string.Empty;
    public string SuggestedCategory { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
