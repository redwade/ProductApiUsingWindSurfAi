namespace WindsurfProductAPI.Models;

public class CatalogInsights
{
    public int TotalProducts { get; set; }
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();
    public decimal AveragePrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public string AIRecommendations { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
