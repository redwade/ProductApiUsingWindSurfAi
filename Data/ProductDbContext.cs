using Microsoft.EntityFrameworkCore;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<PaymentIntent> PaymentIntents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed some sample data
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Wireless Headphones",
                Description = "High-quality wireless headphones with noise cancellation",
                Price = 199.99m,
                Category = "Electronics",
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Smart Watch",
                Description = "Fitness tracking smartwatch with heart rate monitor",
                Price = 299.99m,
                Category = "Electronics",
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 3,
                Name = "Yoga Mat",
                Description = "Premium non-slip yoga mat",
                Price = 49.99m,
                Category = "Sports",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
