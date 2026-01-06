using MassTransit;
using Shared.Contracts;
using System.Net.Http.Json;

namespace NotificationService.Worker.Consumers;

using Shared.Contracts.Events;

/// <summary>
/// Kafka consumer listening to all integration events
/// Simulates sending notifications
/// </summary>
public class IntegrationEventConsumer : 
    IConsumer<IntegrationEvent>,
    IConsumer<OrderConfirmedEvent>,
    IConsumer<OrderCancelledEvent>
{
    private readonly ILogger<IntegrationEventConsumer> _logger;
    private readonly HttpClient _httpClient;

    public IntegrationEventConsumer(ILogger<IntegrationEventConsumer> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Gateway");
    }

    public Task Consume(ConsumeContext<IntegrationEvent> context)
    {
        var @event = context.Message;
        LogEvent(@event);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        LogEvent(context.Message);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        LogEvent(context.Message);
        return Task.CompletedTask;
    }

    private void LogEvent(IntegrationEvent @event)
    {
        var message = GetNotificationMessage(@event);
        _logger.LogInformation(new string('=', 80));
        _logger.LogInformation("📧 NOTIFICATION SENT");
        _logger.LogInformation("Event Type: {EventType}", @event.EventType);
        _logger.LogInformation("Message: {Message}", message);
        _logger.LogInformation(new string('=', 80));

        try
        {
            var logDto = new Shared.Contracts.DTOs.LogMessageDto(
                Source: "NotificationService", 
                Message: $"Notification sent for {@event.EventType}: {message}", 
                Type: "info", 
                Timestamp: DateTime.UtcNow
            );
            _httpClient.PostAsJsonAsync("/api/monitor/send", logDto).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not send monitoring log: {Error}", ex.Message);
        }
    }

    private string GetNotificationMessage(IntegrationEvent @event)
    {
        return @event.EventType switch
        {
            "OrderCreatedEvent" => "New order created! Your order is being processed.",
            "OrderConfirmedEvent" => "Your order is confirmed! Preparation has started.",
            "OrderCancelledEvent" => "Your order has been cancelled.",
            "StockReservedEvent" => "Stock reserved. Proceeding to payment.",
            "StockReservationFailedEvent" => "Sorry, insufficient stock. Order cancelled.",
            "PaymentProcessedEvent" => "Payment processed. Order is being prepared.",
            _ => $"Event received: {@event.EventType}"
        };
    }
}
