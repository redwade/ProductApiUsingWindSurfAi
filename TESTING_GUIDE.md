# TDD/BDD Testing Guide for WindsurfProductAPI

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK installed
- Basic understanding of C# and testing concepts

### Setup

1. **Navigate to the test project**:
   ```bash
   cd WindsurfProductAPI.Tests
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run all tests**:
   ```bash
   dotnet test
   ```

## 📖 What is TDD and BDD?

### Test-Driven Development (TDD)

TDD is a software development approach where you:
1. **Write a failing test** first
2. **Write minimal code** to make the test pass
3. **Refactor** the code while keeping tests green

**Benefits**:
- Better code design
- Higher test coverage
- Fewer bugs
- Living documentation

### Behavior-Driven Development (BDD)

BDD extends TDD by writing tests in natural language that describes business behavior:
- Uses **Gherkin syntax** (Given-When-Then)
- Focuses on **business value**
- Enables **collaboration** between developers, testers, and business stakeholders

## 🎯 TDD Workflow

### Example: Adding a New Feature

Let's say you want to add a discount feature to products.

#### Step 1: Write the Test First (Red)

```csharp
// WindsurfProductAPI.Tests/UnitTests/ProductDiscountTests.cs
using Xunit;
using FluentAssertions;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Tests.UnitTests;

public class ProductDiscountTests
{
    [Fact]
    public void ApplyDiscount_WithValidPercentage_ShouldReducePrice()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Price = 100m
        };

        // Act
        product.ApplyDiscount(10); // 10% discount

        // Assert
        product.Price.Should().Be(90m);
    }

    [Theory]
    [InlineData(10, 90)]
    [InlineData(25, 75)]
    [InlineData(50, 50)]
    public void ApplyDiscount_WithDifferentPercentages_ShouldCalculateCorrectly(
        decimal discountPercent, decimal expectedPrice)
    {
        // Arrange
        var product = new Product { Price = 100m };

        // Act
        product.ApplyDiscount(discountPercent);

        // Assert
        product.Price.Should().Be(expectedPrice);
    }
}
```

#### Step 2: Run the Test (Should Fail)

```bash
dotnet test
# Test will fail because ApplyDiscount method doesn't exist yet
```

#### Step 3: Write Minimal Code to Pass (Green)

```csharp
// WindsurfProductAPI/Models/Product.cs
public class Product
{
    // ... existing properties ...

    public void ApplyDiscount(decimal discountPercent)
    {
        if (discountPercent < 0 || discountPercent > 100)
            throw new ArgumentException("Discount must be between 0 and 100");

        Price = Price * (1 - discountPercent / 100);
    }
}
```

#### Step 4: Run Tests Again (Should Pass)

```bash
dotnet test
# All tests should pass now
```

#### Step 5: Refactor (If Needed)

Improve the code while keeping tests green.

## 🎭 BDD Workflow

### Example: Adding a Product Search Feature

#### Step 1: Write the Feature File

```gherkin
# WindsurfProductAPI.Tests/Features/ProductSearch.feature
Feature: Product Search
    As a customer
    I want to search for products
    So that I can find what I'm looking for quickly

Background:
    Given the following products exist:
        | Name              | Category    | Price  |
        | Laptop Pro        | Electronics | 1299.99|
        | Wireless Mouse    | Electronics | 29.99  |
        | Yoga Mat          | Sports      | 49.99  |
        | Running Shoes     | Sports      | 89.99  |

Scenario: Search by product name
    When I search for "Laptop"
    Then I should see 1 product
    And the product should be "Laptop Pro"

Scenario: Search by category
    When I search in category "Electronics"
    Then I should see 2 products
    And the products should include "Laptop Pro"
    And the products should include "Wireless Mouse"

Scenario: Search with price filter
    When I search for products under 50 dollars
    Then I should see 2 products
    And all products should be priced under 50 dollars

Scenario Outline: Search with different keywords
    When I search for "<Keyword>"
    Then I should see <Count> products

    Examples:
        | Keyword   | Count |
        | Laptop    | 1     |
        | Mouse     | 1     |
        | Sports    | 0     |
```

#### Step 2: Implement Step Definitions

```csharp
// WindsurfProductAPI.Tests/StepDefinitions/ProductSearchSteps.cs
using TechTalk.SpecFlow;
using FluentAssertions;

[Binding]
public class ProductSearchSteps
{
    private readonly HttpClient _client;
    private List<Product>? _searchResults;

    [When(@"I search for ""(.*)""")]
    public async Task WhenISearchFor(string keyword)
    {
        var response = await _client.GetAsync($"/api/products/search?q={keyword}");
        _searchResults = await response.Content.ReadFromJsonAsync<List<Product>>();
    }

    [When(@"I search in category ""(.*)""")]
    public async Task WhenISearchInCategory(string category)
    {
        var response = await _client.GetAsync($"/api/products?category={category}");
        _searchResults = await response.Content.ReadFromJsonAsync<List<Product>>();
    }

    [Then(@"I should see (.*) product")]
    [Then(@"I should see (.*) products")]
    public void ThenIShouldSeeProducts(int count)
    {
        _searchResults.Should().HaveCount(count);
    }

    [Then(@"the product should be ""(.*)""")]
    public void ThenTheProductShouldBe(string productName)
    {
        _searchResults.Should().ContainSingle(p => p.Name == productName);
    }
}
```

#### Step 3: Implement the Feature

```csharp
// WindsurfProductAPI/Program.cs
app.MapGet("/api/products/search", async (string q, ProductDbContext db) =>
{
    var products = await db.Products
        .Where(p => p.Name.Contains(q) || p.Description.Contains(q))
        .ToListAsync();
    
    return Results.Ok(products);
})
.WithName("SearchProducts")
.WithTags("Products");
```

#### Step 4: Run BDD Tests

```bash
dotnet test --filter FullyQualifiedName~ProductSearchSteps
```

## 🛠️ Common Testing Patterns

### 1. Testing API Endpoints

```csharp
[Fact]
public async Task GetProduct_WithValidId_ReturnsProduct()
{
    // Arrange
    var productId = 1;

    // Act
    var response = await _client.GetAsync($"/api/products/{productId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var product = await response.Content.ReadFromJsonAsync<Product>();
    product.Should().NotBeNull();
    product!.Id.Should().Be(productId);
}
```

### 2. Testing with Mock Data

```csharp
[Fact]
public async Task AIService_GeneratesDescription_WhenCalled()
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var service = new WindsurfAIService(mockHttpClient.Object, ...);
    var product = new Product { Name = "Test", Price = 100m };

    // Act
    var description = await service.GenerateMarketingDescription(product);

    // Assert
    description.Should().NotBeNullOrEmpty();
    description.Should().Contain(product.Name);
}
```

### 3. Testing Validation

```csharp
[Theory]
[InlineData(-10)]  // Negative price
[InlineData(0)]    // Zero price
public void Product_WithInvalidPrice_ShouldThrowException(decimal price)
{
    // Arrange & Act
    Action act = () => new Product { Price = price }.Validate();

    // Assert
    act.Should().Throw<ValidationException>();
}
```

### 4. Testing Async Operations

```csharp
[Fact]
public async Task SaveProduct_ShouldPersistToDatabase()
{
    // Arrange
    var product = new Product { Name = "Test", Price = 100m };

    // Act
    await _context.Products.AddAsync(product);
    await _context.SaveChangesAsync();

    // Assert
    var saved = await _context.Products.FindAsync(product.Id);
    saved.Should().NotBeNull();
    saved!.Name.Should().Be("Test");
}
```

## 📊 Test Organization

### Naming Conventions

**Unit Tests**:
```
MethodName_Scenario_ExpectedBehavior
```
Examples:
- `ApplyDiscount_WithValidPercentage_ShouldReducePrice`
- `CreateProduct_WithNullName_ShouldThrowException`

**Integration Tests**:
```
EndpointName_Scenario_ExpectedResult
```
Examples:
- `GetProducts_WithNoFilters_ReturnsAllProducts`
- `CreateProduct_WithInvalidData_ReturnsBadRequest`

**BDD Scenarios**:
```gherkin
Scenario: Action with condition results in outcome
```
Examples:
- `Create a product with valid data`
- `Search for products by category`

## 🎓 Best Practices

### DO ✅

1. **Write tests first** (TDD approach)
2. **Keep tests simple** and focused
3. **Use descriptive names** that explain the test
4. **Test one thing** per test
5. **Use FluentAssertions** for readable assertions
6. **Mock external dependencies**
7. **Clean up after tests** (use IDisposable)

### DON'T ❌

1. **Don't test framework code** (e.g., Entity Framework)
2. **Don't write tests that depend on each other**
3. **Don't use real databases** in unit tests
4. **Don't ignore failing tests**
5. **Don't test private methods** directly
6. **Don't write tests without assertions**

## 🔍 Debugging Tests

### Run a Single Test

```bash
dotnet test --filter "FullyQualifiedName~ProductTests.Product_ShouldInitialize_WithValidData"
```

### Run Tests with Verbose Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Debug in VS Code

Add to `.vscode/launch.json`:
```json
{
    "name": ".NET Core Test",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build",
    "program": "dotnet",
    "args": ["test"],
    "cwd": "${workspaceFolder}/WindsurfProductAPI.Tests",
    "console": "internalConsole"
}
```

## 📈 Measuring Success

### Test Coverage

```bash
# Install coverage tool
dotnet tool install --global dotnet-coverage

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Quality Metrics

- **Coverage**: Aim for 80%+ code coverage
- **Speed**: Unit tests should run in milliseconds
- **Reliability**: Tests should pass consistently
- **Maintainability**: Tests should be easy to understand and update

## 🎯 Next Steps

1. **Practice TDD**: Start with simple features
2. **Write BDD scenarios**: Focus on business value
3. **Refactor existing code**: Add tests to legacy code
4. **Automate**: Set up CI/CD to run tests automatically
5. **Learn more**: Explore advanced testing patterns

## 📚 Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [SpecFlow Documentation](https://docs.specflow.org/)
- [FluentAssertions](https://fluentassertions.com/)
- [Test-Driven Development by Example](https://www.amazon.com/Test-Driven-Development-Kent-Beck/dp/0321146530)
- [The Art of Unit Testing](https://www.manning.com/books/the-art-of-unit-testing-third-edition)

## 💡 Tips

- **Start small**: Begin with simple unit tests
- **Be consistent**: Follow naming conventions
- **Keep learning**: Testing is a skill that improves with practice
- **Get feedback**: Have others review your tests
- **Automate**: Make testing part of your workflow

Happy Testing! 🎉
