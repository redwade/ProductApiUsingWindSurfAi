# WindsurfProductAPI Test Suite

This test suite provides comprehensive TDD (Test-Driven Development) and BDD (Behavior-Driven Development) testing for the WindsurfProductAPI.

## 📋 Table of Contents

- [Overview](#overview)
- [Test Structure](#test-structure)
- [Running Tests](#running-tests)
- [Test Types](#test-types)
- [Writing Tests](#writing-tests)
- [Best Practices](#best-practices)

## 🎯 Overview

The test suite includes:

- **Unit Tests**: Testing individual components in isolation (xUnit + FluentAssertions + Moq)
- **Integration Tests**: Testing API endpoints end-to-end (ASP.NET Core Testing)
- **BDD Tests**: Behavior-driven tests using Gherkin syntax (SpecFlow)

## 📁 Test Structure

```
WindsurfProductAPI.Tests/
├── Features/                    # BDD feature files (Gherkin)
│   ├── ProductManagement.feature
│   └── AIInsights.feature
├── StepDefinitions/            # SpecFlow step definitions
│   ├── ProductManagementSteps.cs
│   └── AIInsightsSteps.cs
├── UnitTests/                  # Unit tests
│   ├── ProductTests.cs
│   └── WindsurfAIServiceTests.cs
├── IntegrationTests/           # Integration tests
│   └── ProductApiTests.cs
├── specflow.json              # SpecFlow configuration
└── README.md                  # This file
```

## 🚀 Running Tests

### Run All Tests

```bash
cd WindsurfProductAPI.Tests
dotnet test
```

### Run Specific Test Category

```bash
# Run only unit tests
dotnet test --filter FullyQualifiedName~UnitTests

# Run only integration tests
dotnet test --filter FullyQualifiedName~IntegrationTests

# Run only BDD tests
dotnet test --filter FullyQualifiedName~StepDefinitions
```

### Run with Detailed Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Generate Test Coverage Report

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 🧪 Test Types

### 1. Unit Tests

**Purpose**: Test individual components in isolation

**Location**: `UnitTests/`

**Example**:
```csharp
[Fact]
public void Product_ShouldInitialize_WithValidData()
{
    // Arrange & Act
    var product = new Product
    {
        Name = "Test Product",
        Price = 99.99m
    };

    // Assert
    product.Name.Should().Be("Test Product");
    product.Price.Should().Be(99.99m);
}
```

**Key Features**:
- Fast execution
- No external dependencies
- Uses Moq for mocking
- FluentAssertions for readable assertions

### 2. Integration Tests

**Purpose**: Test API endpoints end-to-end

**Location**: `IntegrationTests/`

**Example**:
```csharp
[Fact]
public async Task CreateProduct_ShouldReturnCreated()
{
    // Arrange
    var newProduct = new ProductCreateDto
    {
        Name = "Test Product",
        Price = 99.99m
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/products", newProduct);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

**Key Features**:
- Tests complete request/response cycle
- Uses in-memory database
- WebApplicationFactory for hosting
- Real HTTP requests

### 3. BDD Tests (SpecFlow)

**Purpose**: Test business scenarios in human-readable format

**Location**: `Features/` and `StepDefinitions/`

**Example Feature**:
```gherkin
Feature: Product Management
    As a product manager
    I want to manage products in the catalog

Scenario: Create a new product
    When I create a product with the following details:
        | Field       | Value                    |
        | Name        | Wireless Headphones      |
        | Price       | 299.99                   |
    Then the product should be created successfully
    And the product name should be "Wireless Headphones"
```

**Key Features**:
- Business-readable scenarios
- Gherkin syntax
- Reusable step definitions
- Supports data tables and examples

## ✍️ Writing Tests

### Adding a Unit Test

1. Create a new test class in `UnitTests/`
2. Follow the Arrange-Act-Assert pattern
3. Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`

```csharp
[Fact]
public void MethodName_WhenCondition_ShouldExpectedResult()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = MethodUnderTest(input);
    
    // Assert
    result.Should().Be("expected");
}
```

### Adding an Integration Test

1. Create test in `IntegrationTests/`
2. Use `WebApplicationFactory<Program>`
3. Test complete HTTP request/response

```csharp
[Fact]
public async Task EndpointName_Scenario_ExpectedResult()
{
    // Arrange
    var requestData = new { /* data */ };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/endpoint", requestData);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

### Adding a BDD Scenario

1. Add scenario to `.feature` file in `Features/`
2. Implement step definitions in `StepDefinitions/`

**Feature File**:
```gherkin
Scenario: New scenario
    Given some precondition
    When I perform an action
    Then I should see the result
```

**Step Definition**:
```csharp
[Given(@"some precondition")]
public void GivenSomePrecondition()
{
    // Setup code
}

[When(@"I perform an action")]
public async Task WhenIPerformAnAction()
{
    // Action code
}

[Then(@"I should see the result")]
public void ThenIShouldSeeTheResult()
{
    // Assertion code
}
```

## 📚 Best Practices

### General

1. **Keep tests independent**: Each test should run in isolation
2. **Use descriptive names**: Test names should describe what they test
3. **One assertion per test**: Focus on testing one thing at a time
4. **Arrange-Act-Assert**: Follow the AAA pattern consistently

### Unit Tests

1. **Mock external dependencies**: Use Moq to isolate the unit under test
2. **Test edge cases**: Include boundary conditions and error scenarios
3. **Use Theory for parameterized tests**: Test multiple inputs efficiently

```csharp
[Theory]
[InlineData(10, "budget")]
[InlineData(100, "mid-range")]
[InlineData(1000, "premium")]
public void ClassifyPrice_ShouldReturnCorrectSegment(decimal price, string expected)
{
    // Test implementation
}
```

### Integration Tests

1. **Use in-memory database**: Avoid external database dependencies
2. **Clean up after tests**: Ensure database is reset between tests
3. **Test realistic scenarios**: Include authentication, validation, etc.

### BDD Tests

1. **Write from user perspective**: Focus on business value
2. **Keep scenarios simple**: One scenario per business rule
3. **Reuse step definitions**: Create generic, reusable steps
4. **Use examples for variations**: Test multiple inputs with Scenario Outline

## 🔧 Troubleshooting

### Tests Failing to Run

1. Ensure all dependencies are restored:
   ```bash
   dotnet restore
   ```

2. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

### SpecFlow Tests Not Generating

1. Rebuild the project to regenerate feature files:
   ```bash
   dotnet build
   ```

2. Check `specflow.json` configuration

### Database Issues

- Each test uses a unique in-memory database
- Database is automatically cleaned between tests
- No manual cleanup required

## 📊 Test Coverage

To view test coverage:

1. Install coverage tools:
   ```bash
   dotnet tool install --global dotnet-reportgenerator-globaltool
   ```

2. Run tests with coverage:
   ```bash
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
   ```

3. Generate HTML report:
   ```bash
   reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report
   ```

## 🎓 Learning Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [SpecFlow Documentation](https://docs.specflow.org/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)

## 🤝 Contributing

When adding new features:

1. Write tests first (TDD approach)
2. Add BDD scenarios for business features
3. Ensure all tests pass before committing
4. Maintain test coverage above 80%

## 📝 Notes

- Tests use mock AI service (no API key required)
- Integration tests run against in-memory database
- All tests are designed to run in CI/CD pipelines
- No external dependencies required for testing
