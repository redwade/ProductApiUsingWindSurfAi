using Xunit;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WindsurfProductAPI.Models;
using WindsurfProductAPI.Services;

namespace WindsurfProductAPI.Tests.UnitTests;

public class WindsurfAIServiceTests
{
    private readonly Mock<ILogger<WindsurfAIService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public WindsurfAIServiceTests()
    {
        _loggerMock = new Mock<ILogger<WindsurfAIService>>();
        _configurationMock = new Mock<IConfiguration>();
    }

    [Fact]
    public async Task GenerateMarketingDescription_WithoutApiKey_ShouldReturnMockData()
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "A great product",
            Price = 99.99m,
            Category = "Electronics"
        };

        // Act
        var result = await service.GenerateMarketingDescription(product);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Test Product");
        result.Should().Contain("$99.99");
    }

    [Fact]
    public async Task AnalyzeProductPositioning_WithoutApiKey_ShouldReturnMockData()
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var product = new Product
        {
            Id = 1,
            Name = "Premium Laptop",
            Description = "High-end laptop",
            Price = 1500m,
            Category = "Electronics"
        };

        // Act
        var result = await service.AnalyzeProductPositioning(product);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Target Market");
        result.Should().Contain("premium");
    }

    [Theory]
    [InlineData(25, "budget-friendly")]
    [InlineData(100, "mid-range")]
    [InlineData(500, "premium")]
    public async Task AnalyzeProductPositioning_ShouldClassifyByPrice(decimal price, string expectedSegment)
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var product = new Product
        {
            Name = "Test Product",
            Price = price,
            Category = "Electronics"
        };

        // Act
        var result = await service.AnalyzeProductPositioning(product);

        // Assert
        result.Should().Contain(expectedSegment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzePricing_WithoutApiKey_ShouldReturnMockData()
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var product = new Product
        {
            Name = "Test Product",
            Price = 150m,
            Category = "Electronics"
        };

        // Act
        var result = await service.AnalyzePricing(product);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Price Point");
        result.Should().Contain("$150");
    }

    [Theory]
    [InlineData("Smartwatch Pro", "watch", "Electronics")]
    [InlineData("Yoga Mat Premium", "yoga", "Sports & Fitness")]
    [InlineData("The Great Novel", "book", "Books & Media")]
    public async Task SuggestCategory_ShouldSuggestBasedOnKeywords(string name, string keyword, string expectedCategory)
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var product = new Product
        {
            Name = name,
            Description = $"A {keyword} product",
            Category = "General"
        };

        // Act
        var result = await service.SuggestCategory(product);

        // Assert
        result.Should().Be(expectedCategory);
    }

    [Fact]
    public async Task GenerateProductInsights_ShouldReturnCompleteInsights()
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Category = "Electronics"
        };

        // Act
        var result = await service.GenerateProductInsights(product);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(1);
        result.ProductName.Should().Be("Test Product");
        result.MarketingDescription.Should().NotBeNullOrEmpty();
        result.Positioning.Should().NotBeNullOrEmpty();
        result.PricingAnalysis.Should().NotBeNullOrEmpty();
        result.SuggestedCategory.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateCatalogInsights_WithEmptyList_ShouldHandleGracefully()
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var products = new List<Product>();

        // Act
        var result = await service.GenerateCatalogInsights(products);

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(0);
        result.AveragePrice.Should().Be(0);
        result.MinPrice.Should().Be(0);
        result.MaxPrice.Should().Be(0);
    }

    [Fact]
    public async Task GenerateCatalogInsights_WithProducts_ShouldCalculateStatistics()
    {
        // Arrange
        var httpClient = new HttpClient();
        _configurationMock.Setup(c => c["WindsurfAI:ApiKey"]).Returns((string?)null);
        
        var service = new WindsurfAIService(httpClient, _configurationMock.Object, _loggerMock.Object);
        var products = new List<Product>
        {
            new Product { Name = "Product 1", Price = 100m, Category = "Electronics" },
            new Product { Name = "Product 2", Price = 200m, Category = "Electronics" },
            new Product { Name = "Product 3", Price = 150m, Category = "Books" }
        };

        // Act
        var result = await service.GenerateCatalogInsights(products);

        // Assert
        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(3);
        result.AveragePrice.Should().Be(150m);
        result.MinPrice.Should().Be(100m);
        result.MaxPrice.Should().Be(200m);
        result.CategoryDistribution.Should().ContainKey("Electronics");
        result.CategoryDistribution["Electronics"].Should().Be(2);
        result.CategoryDistribution.Should().ContainKey("Books");
        result.CategoryDistribution["Books"].Should().Be(1);
    }
}
