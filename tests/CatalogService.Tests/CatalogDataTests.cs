using CatalogService.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CatalogService.Tests;

public class CatalogDataTests
{
    private CatalogDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new CatalogDbContext(options);
    }

    [Fact]
    public async Task Should_Save_Product_With_Category()
    {
        // Arrange
        var context = GetContext();
        var category = new Category { Id = "test-cat", Name = "Test Category" };
        var product = new Product 
        { 
            Id = "test-prod", 
            Name = "Test Product", 
            Price = 100, 
            CategoryId = "test-cat" 
        };

        // Act
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Assert
        var result = await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == "test-prod");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Product");
        result.Category.Should().NotBeNull();
        result.Category!.Name.Should().Be("Test Category");
    }
}
