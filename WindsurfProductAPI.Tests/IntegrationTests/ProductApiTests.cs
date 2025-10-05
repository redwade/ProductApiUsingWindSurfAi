using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Tests.IntegrationTests;

public class ProductApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ProductDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        products.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProduct_ShouldReturnCreated()
    {
        // Arrange
        var newProduct = new ProductCreateDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Category = "Electronics"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", newProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Test Product");
        product.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ShouldReturnOk()
    {
        // Arrange - Create a product first
        var newProduct = new ProductCreateDto
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Category = "Electronics"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", newProduct);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>();

        // Act
        var response = await _client.GetAsync($"/api/products/{createdProduct!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(createdProduct.Id);
    }

    [Fact]
    public async Task GetProductById_NonExistingProduct_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/products/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_ExistingProduct_ShouldReturnOk()
    {
        // Arrange - Create a product first
        var newProduct = new ProductCreateDto
        {
            Name = "Original Product",
            Description = "Original Description",
            Price = 99.99m,
            Category = "Electronics"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", newProduct);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>();

        var updatedProduct = new ProductCreateDto
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 149.99m,
            Category = "Electronics"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", updatedProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Updated Product");
        product.Price.Should().Be(149.99m);
    }

    [Fact]
    public async Task UpdateProduct_NonExistingProduct_ShouldReturnNotFound()
    {
        // Arrange
        var updatedProduct = new ProductCreateDto
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 149.99m,
            Category = "Electronics"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/products/99999", updatedProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_ExistingProduct_ShouldReturnNoContent()
    {
        // Arrange - Create a product first
        var newProduct = new ProductCreateDto
        {
            Name = "Product to Delete",
            Description = "Will be deleted",
            Price = 99.99m,
            Category = "Electronics"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/products", newProduct);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<Product>();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{createdProduct!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_NonExistingProduct_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/products/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
