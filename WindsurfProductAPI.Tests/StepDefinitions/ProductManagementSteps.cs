using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Tests.StepDefinitions;

[Binding]
public class ProductManagementSteps : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private Product? _currentProduct;
    private List<Product>? _products;

    public ProductManagementSteps()
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

    [Given(@"the API is running")]
    public void GivenTheAPIIsRunning()
    {
        // API is running via the WebApplicationFactory
        _client.Should().NotBeNull();
    }

    [Given(@"the database is empty")]
    public async Task GivenTheDatabaseIsEmpty()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        context.Products.RemoveRange(context.Products);
        await context.SaveChangesAsync();
    }

    [When(@"I create a product with the following details:")]
    public async Task WhenICreateAProductWithTheFollowingDetails(Table table)
    {
        var productDto = new ProductCreateDto
        {
            Name = table.Rows[0]["Value"],
            Description = table.Rows[1]["Value"],
            Price = decimal.Parse(table.Rows[2]["Value"]),
            Category = table.Rows[3]["Value"]
        };

        _response = await _client.PostAsJsonAsync("/api/products", productDto);
        if (_response.IsSuccessStatusCode)
        {
            _currentProduct = await _response.Content.ReadFromJsonAsync<Product>();
        }
    }

    [When(@"I create a product with name ""(.*)"" and price (.*)")]
    public async Task WhenICreateAProductWithNameAndPrice(string name, decimal price)
    {
        var productDto = new ProductCreateDto
        {
            Name = name,
            Description = "Test product",
            Price = price,
            Category = "General"
        };

        _response = await _client.PostAsJsonAsync("/api/products", productDto);
        if (_response.IsSuccessStatusCode)
        {
            _currentProduct = await _response.Content.ReadFromJsonAsync<Product>();
        }
    }

    [Given(@"the following products exist:")]
    public async Task GivenTheFollowingProductsExist(Table table)
    {
        foreach (var row in table.Rows)
        {
            var productDto = new ProductCreateDto
            {
                Name = row["Name"],
                Description = row["Description"],
                Price = decimal.Parse(row["Price"]),
                Category = row["Category"]
            };

            await _client.PostAsJsonAsync("/api/products", productDto);
        }
    }

    [Given(@"a product exists with the following details:")]
    public async Task GivenAProductExistsWithTheFollowingDetails(Table table)
    {
        var productDto = new ProductCreateDto
        {
            Name = table.Rows[0]["Value"],
            Description = table.Rows[1]["Value"],
            Price = decimal.Parse(table.Rows[2]["Value"]),
            Category = table.Rows[3]["Value"]
        };

        var response = await _client.PostAsJsonAsync("/api/products", productDto);
        _currentProduct = await response.Content.ReadFromJsonAsync<Product>();
    }

    [Given(@"a product exists with name ""(.*)""")]
    public async Task GivenAProductExistsWithName(string name)
    {
        var productDto = new ProductCreateDto
        {
            Name = name,
            Description = "Test product",
            Price = 99.99m,
            Category = "General"
        };

        var response = await _client.PostAsJsonAsync("/api/products", productDto);
        _currentProduct = await response.Content.ReadFromJsonAsync<Product>();
    }

    [When(@"I request all products")]
    public async Task WhenIRequestAllProducts()
    {
        _response = await _client.GetAsync("/api/products");
        if (_response.IsSuccessStatusCode)
        {
            _products = await _response.Content.ReadFromJsonAsync<List<Product>>();
        }
    }

    [When(@"I update the product with:")]
    public async Task WhenIUpdateTheProductWith(Table table)
    {
        var productDto = new ProductCreateDto
        {
            Name = table.Rows[0]["Value"],
            Description = table.Rows[1]["Value"],
            Price = decimal.Parse(table.Rows[2]["Value"]),
            Category = table.Rows[3]["Value"]
        };

        _response = await _client.PutAsJsonAsync($"/api/products/{_currentProduct!.Id}", productDto);
        if (_response.IsSuccessStatusCode)
        {
            _currentProduct = await _response.Content.ReadFromJsonAsync<Product>();
        }
    }

    [When(@"I delete the product")]
    public async Task WhenIDeleteTheProduct()
    {
        _response = await _client.DeleteAsync($"/api/products/{_currentProduct!.Id}");
    }

    [When(@"I request a product with ID (.*)")]
    public async Task WhenIRequestAProductWithID(int id)
    {
        _response = await _client.GetAsync($"/api/products/{id}");
    }

    [Then(@"the product should be created successfully")]
    public void ThenTheProductShouldBeCreatedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.Created);
        _currentProduct.Should().NotBeNull();
    }

    [Then(@"the product should have an ID")]
    public void ThenTheProductShouldHaveAnID()
    {
        _currentProduct!.Id.Should().BeGreaterThan(0);
    }

    [Then(@"the product name should be ""(.*)""")]
    public void ThenTheProductNameShouldBe(string expectedName)
    {
        _currentProduct!.Name.Should().Be(expectedName);
    }

    [Then(@"the product price should be (.*)")]
    public void ThenTheProductPriceShouldBe(decimal expectedPrice)
    {
        _currentProduct!.Price.Should().Be(expectedPrice);
    }

    [Then(@"I should receive (.*) products")]
    public void ThenIShouldReceiveProducts(int count)
    {
        _products.Should().NotBeNull();
        _products!.Count.Should().Be(count);
    }

    [Then(@"the products should include ""(.*)""")]
    public void ThenTheProductsShouldInclude(string productName)
    {
        _products.Should().Contain(p => p.Name == productName);
    }

    [Then(@"the product should be updated successfully")]
    public void ThenTheProductShouldBeUpdatedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _currentProduct.Should().NotBeNull();
    }

    [Then(@"the product should be deleted successfully")]
    public void ThenTheProductShouldBeDeletedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then(@"the product should not be found when retrieved")]
    public async Task ThenTheProductShouldNotBeFoundWhenRetrieved()
    {
        var response = await _client.GetAsync($"/api/products/{_currentProduct!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Then(@"I should receive a not found response")]
    public void ThenIShouldReceiveANotFoundResponse()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
