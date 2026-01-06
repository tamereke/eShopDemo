using FluentAssertions;
using InventoryService.Api.Models;
using InventoryService.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryService.Tests.Services;

public class InventoryServiceTests
{
    private readonly Mock<ILogger<InventoryService.Api.Services.InventoryService>> _mockLogger;
    private readonly InventoryService.Api.Services.InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        _mockLogger = new Mock<ILogger<InventoryService.Api.Services.InventoryService>>();
        _inventoryService = new InventoryService.Api.Services.InventoryService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetProductAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var productId = "product-1";

        // Act
        var result = await _inventoryService.GetProductAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.AvailableStock.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProductAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = "non-existent-product";

        // Act
        var result = await _inventoryService.GetProductAsync(productId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReserveStockAsync_ShouldReturnTrue_WhenStockIsAvailable()
    {
        // Arrange
        var productId = "product-1";
        var product = await _inventoryService.GetProductAsync(productId);
        var initialStock = product!.AvailableStock;
        var quantityToReserve = 5;

        // Act
        var result = await _inventoryService.ReserveStockAsync(productId, quantityToReserve);

        // Assert
        result.Should().BeTrue();
        
        var updatedProduct = await _inventoryService.GetProductAsync(productId);
        updatedProduct!.AvailableStock.Should().Be(initialStock - quantityToReserve);
        updatedProduct.ReservedStock.Should().Be(quantityToReserve);
    }

    [Fact]
    public async Task ReserveStockAsync_ShouldReturnFalse_WhenInsufficientStock()
    {
        // Arrange
        var productId = "product-1";
        var quantityToReserve = 10000; // Too much

        // Act
        var result = await _inventoryService.ReserveStockAsync(productId, quantityToReserve);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReserveStockAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = "non-existent-product";

        // Act
        var result = await _inventoryService.ReserveStockAsync(productId, 5);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldUpdateStock_Successfully()
    {
        // Arrange
        var productId = "product-1";
        var newStock = 250;

        // Act
        await _inventoryService.UpdateStockAsync(productId, newStock);

        // Assert
        var product = await _inventoryService.GetProductAsync(productId);
        product!.AvailableStock.Should().Be(newStock);
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        // Act
        var products = await _inventoryService.GetAllProductsAsync();

        // Assert
        products.Should().NotBeEmpty();
        products.Should().HaveCountGreaterThan(0);
    }
}
