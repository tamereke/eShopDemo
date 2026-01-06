using InventoryService.Api.Models;
using System.Collections.Concurrent;

namespace InventoryService.Api.Services;

public class InventoryService : IInventoryService
{
    private readonly ConcurrentDictionary<string, InventoryItem> _inventory = new();
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
        SeedInitialData();
    }

    private void SeedInitialData()
    {
        _inventory.TryAdd("product-1", new InventoryItem
        {
            ProductId = "product-1",
            ProductName = "Laptop",
            AvailableStock = 100,
            ReservedStock = 0
        });

        _inventory.TryAdd("product-2", new InventoryItem
        {
            ProductId = "product-2",
            ProductName = "Mouse",
            AvailableStock = 500,
            ReservedStock = 0
        });

        _inventory.TryAdd("product-3", new InventoryItem
        {
            ProductId = "product-3",
            ProductName = "Keyboard",
            AvailableStock = 300,
            ReservedStock = 0
        });

        _logger.LogInformation("Inventory seeded with {Count} products", _inventory.Count);
    }

    public Task<InventoryItem?> GetProductAsync(string productId)
    {
        _inventory.TryGetValue(productId, out var item);
        return Task.FromResult(item);
    }

    public Task<bool> ReserveStockAsync(string productId, int quantity)
    {
        if (!_inventory.TryGetValue(productId, out var item))
        {
            _logger.LogWarning("Product not found: {ProductId}", productId);
            return Task.FromResult(false);
        }

        if (!item.CanReserve(quantity))
        {
            _logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}",
                productId, item.AvailableStock, quantity);
            return Task.FromResult(false);
        }

        item.Reserve(quantity);
        _logger.LogInformation("Reserved {Quantity} units of {ProductId}. Remaining: {Remaining}",
            quantity, productId, item.AvailableStock);

        return Task.FromResult(true);
    }

    public Task UpdateStockAsync(string productId, int newStock)
    {
        if (_inventory.TryGetValue(productId, out var item))
        {
            item.AvailableStock = newStock;
            _logger.LogInformation("Updated stock for {ProductId} to {Stock}", productId, newStock);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<InventoryItem>> GetAllProductsAsync()
    {
        return Task.FromResult(_inventory.Values.AsEnumerable());
    }
}

public interface IInventoryService
{
    Task<InventoryItem?> GetProductAsync(string productId);
    Task<bool> ReserveStockAsync(string productId, int quantity);
    Task UpdateStockAsync(string productId, int newStock);
    Task<IEnumerable<InventoryItem>> GetAllProductsAsync();
}
