namespace Shared.Contracts.DTOs;

public record CreateOrderRequest
{
    public string CustomerId { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}

public record OrderDto
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record OrderItemDto
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public record ProductDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string ImageUri { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
}

public record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string ImageUri { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
}

public record UpdateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string ImageUri { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
}

public record InventoryDto
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int AvailableStock { get; init; }
    public int ReservedStock { get; init; }
}

public record CategoryDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record PaymentDto
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; }
}

public record BasketDto
{
    public string CustomerId { get; init; } = string.Empty;
    public List<BasketItemDto> Items { get; init; } = new();
    public decimal TotalPrice => Items.Sum(i => i.UnitPrice * i.Quantity);
}

public record BasketItemDto
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

public record LogMessageDto(string Source, string Message, string Type, DateTime Timestamp);
