namespace Shared.Contracts.Events;

using Shared.Contracts.DTOs;

public record OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
    public DateTime OrderDate { get; init; }
}

public record StockReservedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public bool IsReserved { get; init; }
    public string? ReservationFailureReason { get; init; }
}

public record StockReservationFailedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record PaymentProcessedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public bool IsSuccess { get; init; }
    public decimal Amount { get; init; }
    public string? FailureReason { get; init; }
    public DateTime ProcessedAt { get; init; }
}

public record OrderConfirmedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public DateTime ConfirmedAt { get; init; }
}

public record OrderCancelledEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}
