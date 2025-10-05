using TechTalk.SpecFlow;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Tests.StepDefinitions;

[Binding]
public class AIInsightsSteps : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private Product? _currentProduct;
    private AIInsightResponse? _aiInsights;
    private string? _marketingDescription;
    private string? _positioning;
    private string? _pricingAnalysis;
    private string? _suggestedCategory;
    private CatalogInsights? _catalogInsights;

    public AIInsightsSteps()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ProductDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [Given(@"a product exists with price (.*)")]
    public async Task GivenAProductExistsWithPrice(decimal price)
    {
        var productDto = new ProductCreateDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = price,
            Category = "Electronics"
        };

        var response = await _client.PostAsJsonAsync("/api/products", productDto);
        _currentProduct = await response.Content.ReadFromJsonAsync<Product>();
    }

    [When(@"I request a marketing description for the product")]
    public async Task WhenIRequestAMarketingDescriptionForTheProduct()
    {
        _response = await _client.PostAsync($"/api/products/{_currentProduct!.Id}/marketing-description", null);
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            _marketingDescription = jsonDoc.RootElement.GetProperty("marketingDescription").GetString();
        }
    }

    [When(@"I request AI insights for the product")]
    public async Task WhenIRequestAIInsightsForTheProduct()
    {
        _response = await _client.PostAsync($"/api/products/{_currentProduct!.Id}/ai-insights", null);
        if (_response.IsSuccessStatusCode)
        {
            _aiInsights = await _response.Content.ReadFromJsonAsync<AIInsightResponse>();
        }
    }

    [When(@"I request positioning analysis for the product")]
    public async Task WhenIRequestPositioningAnalysisForTheProduct()
    {
        _response = await _client.PostAsync($"/api/products/{_currentProduct!.Id}/positioning", null);
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            _positioning = jsonDoc.RootElement.GetProperty("positioning").GetString();
        }
    }

    [When(@"I request pricing analysis for the product")]
    public async Task WhenIRequestPricingAnalysisForTheProduct()
    {
        _response = await _client.PostAsync($"/api/products/{_currentProduct!.Id}/pricing-analysis", null);
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            _pricingAnalysis = jsonDoc.RootElement.GetProperty("pricingAnalysis").GetString();
        }
    }

    [When(@"I request category suggestion for the product")]
    public async Task WhenIRequestCategorySuggestionForTheProduct()
    {
        _response = await _client.PostAsync($"/api/products/{_currentProduct!.Id}/suggest-category", null);
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            _suggestedCategory = jsonDoc.RootElement.GetProperty("suggestedCategory").GetString();
        }
    }

    [When(@"I request catalog insights")]
    public async Task WhenIRequestCatalogInsights()
    {
        _response = await _client.PostAsync("/api/catalog/ai-insights", null);
        if (_response.IsSuccessStatusCode)
        {
            _catalogInsights = await _response.Content.ReadFromJsonAsync<CatalogInsights>();
        }
    }

    [Then(@"the marketing description should be generated")]
    public void ThenTheMarketingDescriptionShouldBeGenerated()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _marketingDescription.Should().NotBeNullOrEmpty();
    }

    [Then(@"the description should contain the product name")]
    public void ThenTheDescriptionShouldContainTheProductName()
    {
        _marketingDescription.Should().Contain(_currentProduct!.Name);
    }

    [Then(@"the description should contain the price")]
    public void ThenTheDescriptionShouldContainThePrice()
    {
        _marketingDescription.Should().Contain(_currentProduct!.Price.ToString());
    }

    [Then(@"the AI insights should include marketing description")]
    public void ThenTheAIInsightsShouldIncludeMarketingDescription()
    {
        _aiInsights.Should().NotBeNull();
        _aiInsights!.MarketingDescription.Should().NotBeNullOrEmpty();
    }

    [Then(@"the AI insights should include positioning analysis")]
    public void ThenTheAIInsightsShouldIncludePositioningAnalysis()
    {
        _aiInsights!.Positioning.Should().NotBeNullOrEmpty();
    }

    [Then(@"the AI insights should include pricing analysis")]
    public void ThenTheAIInsightsShouldIncludePricingAnalysis()
    {
        _aiInsights!.PricingAnalysis.Should().NotBeNullOrEmpty();
    }

    [Then(@"the AI insights should include category suggestion")]
    public void ThenTheAIInsightsShouldIncludeCategorySuggestion()
    {
        _aiInsights!.SuggestedCategory.Should().NotBeNullOrEmpty();
    }

    [Then(@"the positioning should indicate ""(.*)"" segment")]
    public void ThenThePositioningShouldIndicateSegment(string segment)
    {
        _positioning.Should().NotBeNullOrEmpty();
        _positioning.Should().Contain(segment, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"the positioning should include target market information")]
    public void ThenThePositioningShouldIncludeTargetMarketInformation()
    {
        _positioning.Should().Contain("Target Market", StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"the pricing analysis should classify it as ""(.*)""")]
    public void ThenThePricingAnalysisShouldClassifyItAs(string classification)
    {
        _pricingAnalysis.Should().NotBeNullOrEmpty();
        _pricingAnalysis.Should().Contain(classification, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"the insights should show (.*) total products")]
    public void ThenTheInsightsShouldShowTotalProducts(int count)
    {
        _catalogInsights.Should().NotBeNull();
        _catalogInsights!.TotalProducts.Should().Be(count);
    }

    [Then(@"the insights should show (.*) categories")]
    public void ThenTheInsightsShouldShowCategories(int count)
    {
        _catalogInsights!.CategoryDistribution.Should().NotBeNull();
        _catalogInsights.CategoryDistribution.Count.Should().Be(count);
    }

    [Then(@"the insights should include average price")]
    public void ThenTheInsightsShouldIncludeAveragePrice()
    {
        _catalogInsights!.AveragePrice.Should().BeGreaterThan(0);
    }

    [Then(@"the insights should include recommendations")]
    public void ThenTheInsightsShouldIncludeRecommendations()
    {
        _catalogInsights!.AIRecommendations.Should().NotBeNullOrEmpty();
    }

    [Then(@"a category should be suggested")]
    public void ThenACategoryShouldBeSuggested()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _suggestedCategory.Should().NotBeNullOrEmpty();
    }

    [Then(@"the suggested category should be relevant to the product")]
    public void ThenTheSuggestedCategoryShouldBeRelevantToTheProduct()
    {
        _suggestedCategory.Should().NotBeNullOrEmpty();
        // The suggested category should be a non-empty string
        _suggestedCategory!.Length.Should().BeGreaterThan(0);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
