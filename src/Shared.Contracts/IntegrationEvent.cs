using System.Text.Json.Serialization;
using Shared.Contracts.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(OrderCreatedEvent), "OrderCreatedEvent")]
[JsonDerivedType(typeof(StockReservedEvent), "StockReservedEvent")]
[JsonDerivedType(typeof(StockReservationFailedEvent), "StockReservationFailedEvent")]
[JsonDerivedType(typeof(PaymentProcessedEvent), "PaymentProcessedEvent")]
[JsonDerivedType(typeof(OrderConfirmedEvent), "OrderConfirmedEvent")]
[JsonDerivedType(typeof(OrderCancelledEvent), "OrderCancelledEvent")]
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; }

    protected IntegrationEvent()
    {
        EventType = GetType().Name;
    }
}
