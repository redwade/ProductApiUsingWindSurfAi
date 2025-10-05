using Xunit;
using FluentAssertions;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Tests.UnitTests;

public class ProductTests
{
    [Fact]
    public void Product_ShouldInitialize_WithValidData()
    {
        // Arrange & Act
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Category = "Electronics",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        product.Id.Should().Be(1);
        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("Test Description");
        product.Price.Should().Be(99.99m);
        product.Category.Should().Be("Electronics");
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("Laptop", "Electronics")]
    [InlineData("Book", "Media")]
    [InlineData("Shirt", "Clothing")]
    public void Product_ShouldAccept_DifferentCategories(string name, string category)
    {
        // Arrange & Act
        var product = new Product
        {
            Name = name,
            Category = category
        };

        // Assert
        product.Name.Should().Be(name);
        product.Category.Should().Be(category);
    }

    [Fact]
    public void Product_ShouldStore_AIGeneratedData()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Price = 99.99m
        };

        // Act
        product.AIGeneratedDescription = "AI generated marketing copy";
        product.AIPositioning = "Premium market segment";
        product.AIPricingAnalysis = "Competitively priced";
        product.AICategory = "Tech Gadgets";
        product.LastAIAnalysis = DateTime.UtcNow;

        // Assert
        product.AIGeneratedDescription.Should().Be("AI generated marketing copy");
        product.AIPositioning.Should().Be("Premium market segment");
        product.AIPricingAnalysis.Should().Be("Competitively priced");
        product.AICategory.Should().Be("Tech Gadgets");
        product.LastAIAnalysis.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10.50)]
    [InlineData(999.99)]
    [InlineData(1000000)]
    public void Product_ShouldAccept_ValidPrices(decimal price)
    {
        // Arrange & Act
        var product = new Product { Price = price };

        // Assert
        product.Price.Should().Be(price);
    }
}
