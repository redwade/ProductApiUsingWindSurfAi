using Microsoft.EntityFrameworkCore;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;
using WindsurfProductAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Windsurf Product API",
        Version = "v1",
        Description = "A .NET 8 Web API with Windsurf AI integration for intelligent product catalog management",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });
});

// Configure Entity Framework with In-Memory Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseInMemoryDatabase("ProductsDb"));

// Configure HttpClient for Windsurf AI Service
builder.Services.AddHttpClient<IWindsurfAIService, WindsurfAIService>(client =>
{
    var baseUrl = builder.Configuration["WindsurfAI:BaseUrl"] ?? "https://api.windsurf.ai/v1";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Windsurf Product API v1");
    options.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
});

app.UseCors();
app.UseHttpsRedirection();

// ============================================
// PRODUCT CRUD ENDPOINTS
// ============================================

app.MapGet("/api/products", async (ProductDbContext db) =>
{
    var products = await db.Products.ToListAsync();
    return Results.Ok(products);
})
.WithName("GetAllProducts")
.WithTags("Products")
.WithOpenApi(operation => new(operation)
{
    Summary = "Get all products",
    Description = "Retrieves all products from the catalog"
});

app.MapGet("/api/products/{id}", async (int id, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithTags("Products")
.WithOpenApi(operation => new(operation)
{
    Summary = "Get product by ID",
    Description = "Retrieves a specific product by its ID"
});

app.MapPost("/api/products", async (ProductCreateDto productDto, ProductDbContext db) =>
{
    var product = new Product
    {
        Name = productDto.Name,
        Description = productDto.Description,
        Price = productDto.Price,
        Category = productDto.Category,
        CreatedAt = DateTime.UtcNow
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/api/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithTags("Products")
.WithOpenApi(operation => new(operation)
{
    Summary = "Create a new product",
    Description = "Adds a new product to the catalog"
});

app.MapPut("/api/products/{id}", async (int id, ProductCreateDto productDto, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = productDto.Name;
    product.Description = productDto.Description;
    product.Price = productDto.Price;
    product.Category = productDto.Category;

    await db.SaveChangesAsync();
    return Results.Ok(product);
})
.WithName("UpdateProduct")
.WithTags("Products")
.WithOpenApi(operation => new(operation)
{
    Summary = "Update a product",
    Description = "Updates an existing product's information"
});

app.MapDelete("/api/products/{id}", async (int id, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithTags("Products")
.WithOpenApi(operation => new(operation)
{
    Summary = "Delete a product",
    Description = "Removes a product from the catalog"
});

// ============================================
// AI-POWERED ENDPOINTS
// ============================================

app.MapPost("/api/products/{id}/ai-insights", async (int id, ProductDbContext db, IWindsurfAIService aiService) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    try
    {
        var insights = await aiService.GenerateProductInsights(product);
        
        // Update product with AI-generated insights
        product.AIGeneratedDescription = insights.MarketingDescription;
        product.AIPositioning = insights.Positioning;
        product.AIPricingAnalysis = insights.PricingAnalysis;
        product.AICategory = insights.SuggestedCategory;
        product.LastAIAnalysis = DateTime.UtcNow;
        
        await db.SaveChangesAsync();
        
        return Results.Ok(insights);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "AI Analysis Failed"
        );
    }
})
.WithName("GenerateProductInsights")
.WithTags("AI Features")
.WithOpenApi(operation => new(operation)
{
    Summary = "Generate AI insights for a product",
    Description = "Uses Windsurf AI to generate marketing description, positioning analysis, pricing insights, and category suggestions"
});

app.MapPost("/api/products/{id}/marketing-description", async (int id, ProductDbContext db, IWindsurfAIService aiService) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    try
    {
        var description = await aiService.GenerateMarketingDescription(product);
        product.AIGeneratedDescription = description;
        product.LastAIAnalysis = DateTime.UtcNow;
        await db.SaveChangesAsync();
        
        return Results.Ok(new { ProductId = id, MarketingDescription = description });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GenerateMarketingDescription")
.WithTags("AI Features")
.WithOpenApi(operation => new(operation)
{
    Summary = "Generate marketing description",
    Description = "Creates a compelling marketing description for the product using AI"
});

app.MapPost("/api/products/{id}/positioning", async (int id, ProductDbContext db, IWindsurfAIService aiService) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    try
    {
        var positioning = await aiService.AnalyzeProductPositioning(product);
        product.AIPositioning = positioning;
        product.LastAIAnalysis = DateTime.UtcNow;
        await db.SaveChangesAsync();
        
        return Results.Ok(new { ProductId = id, Positioning = positioning });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("AnalyzeProductPositioning")
.WithTags("AI Features")
.WithOpenApi(operation => new(operation)
{
    Summary = "Analyze product positioning",
    Description = "Provides AI-powered insights on product positioning and target market"
});

app.MapPost("/api/products/{id}/pricing-analysis", async (int id, ProductDbContext db, IWindsurfAIService aiService) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    try
    {
        var analysis = await aiService.AnalyzePricing(product);
        product.AIPricingAnalysis = analysis;
        product.LastAIAnalysis = DateTime.UtcNow;
        await db.SaveChangesAsync();
        
        return Results.Ok(new { ProductId = id, PricingAnalysis = analysis });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("AnalyzePricing")
.WithTags("AI Features")
.WithOpenApi(operation => new(operation)
{
    Summary = "Analyze pricing strategy",
    Description = "Provides AI-powered pricing analysis and recommendations"
});

app.MapPost("/api/products/{id}/suggest-category", async (int id, ProductDbContext db, IWindsurfAIService aiService) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    try
    {
        var category = await aiService.SuggestCategory(product);
        product.AICategory = category;
        product.LastAIAnalysis = DateTime.UtcNow;
        await db.SaveChangesAsync();
        
        return Results.Ok(new { ProductId = id, SuggestedCategory = category, CurrentCategory = product.Category });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("SuggestCategory")
.WithTags("AI Features")
.WithOpenApi(operation => new(operation)
{
    Summary = "Suggest product category",
    Description = "Uses AI to suggest the most appropriate category for the product"
});

app.MapPost("/api/catalog/ai-insights", async (ProductDbContext db, IWindsurfAIService aiService) =>
{
    try
    {
        var products = await db.Products.ToListAsync();
        var insights = await aiService.GenerateCatalogInsights(products);
        return Results.Ok(insights);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("GenerateCatalogInsights")
.WithTags("AI Features")
.WithOpenApi(operation => new(operation)
{
    Summary = "Generate catalog-wide insights",
    Description = "Analyzes the entire product catalog and provides actionable business insights"
});

app.MapPost("/api/catalog/batch-analyze", async (ProductDbContext db, IWindsurfAIService aiService) =>
{
    try
    {
        var products = await db.Products.ToListAsync();
        var results = new List<object>();

        foreach (var product in products)
        {
            try
            {
                var insights = await aiService.GenerateProductInsights(product);
                
                product.AIGeneratedDescription = insights.MarketingDescription;
                product.AIPositioning = insights.Positioning;
                product.AIPricingAnalysis = insights.PricingAnalysis;
                product.AICategory = insights.SuggestedCategory;
                product.LastAIAnalysis = DateTime.UtcNow;
                
                results.Add(new { ProductId = product.Id, ProductName = product.Name, Status = "Success" });
            }
            catch (Exception ex)
            {
                results.Add(new { ProductId = product.Id, ProductName = product.Name, Status = "Failed", Error = ex.Message });
            }
        }

        await db.SaveChangesAsync();
        
        return Results.Ok(new 
        { 
            TotalProducts = products.Count,
            Analyzed = results.Count(r => ((dynamic)r).Status == "Success"),
            Failed = results.Count(r => ((dynamic)r).Status == "Failed"),
            Results = results 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithName("BatchAnalyzeCatalog")
.WithTags("AI Features")
.WithOpenApi(operation => new(operation)
{
    Summary = "Batch analyze entire catalog",
    Description = "Automatically categorizes and analyzes all products in the catalog using AI"
});

// ============================================
// HEALTH CHECK
// ============================================

app.MapGet("/api/health", () => Results.Ok(new 
{ 
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0",
    AIEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINDSURF_API_KEY"))
}))
.WithName("HealthCheck")
.WithTags("System")
.WithOpenApi(operation => new(operation)
{
    Summary = "Health check",
    Description = "Returns the API health status and configuration"
});

app.Run();
