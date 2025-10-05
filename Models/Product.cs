namespace WindsurfProductAPI.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? AIGeneratedDescription { get; set; }
    public string? AIPositioning { get; set; }
    public string? AIPricingAnalysis { get; set; }
    public string? AICategory { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAIAnalysis { get; set; }
}
