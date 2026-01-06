using InventoryService.Api.Services;
using MassTransit;
using Shared.Contracts.Events;

namespace InventoryService.Api.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(
        IInventoryService inventoryService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderCreatedEventConsumer> logger)
    {
        _inventoryService = inventoryService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var orderEvent = context.Message;
        _logger.LogInformation("Processing OrderCreatedEvent for Order: {OrderId}", orderEvent.OrderId);

        try
        {
            var allReserved = true;
            var failureReason = string.Empty;

            foreach (var item in orderEvent.Items)
            {
                var canReserve = await _inventoryService.ReserveStockAsync(item.ProductId, item.Quantity);
                
                if (!canReserve)
                {
                    allReserved = false;
                    failureReason = $"Insufficient stock for product {item.ProductName}";
                    _logger.LogWarning("Stock reservation failed for product {ProductId} in order {OrderId}",
                        item.ProductId, orderEvent.OrderId);
                    break;
                }
            }

            if (allReserved)
            {
                var stockReservedEvent = new StockReservedEvent
                {
                    OrderId = orderEvent.OrderId,
                    CustomerId = orderEvent.CustomerId,
                    TotalAmount = orderEvent.TotalAmount,
                    IsReserved = true
                };

                await _publishEndpoint.Publish(stockReservedEvent);
                _logger.LogInformation("Stock reserved for order {OrderId}. Event published.", orderEvent.OrderId);
            }
            else
            {
                var failedEvent = new StockReservationFailedEvent
                {
                    OrderId = orderEvent.OrderId,
                    Reason = failureReason
                };

                await _publishEndpoint.Publish(failedEvent);
                _logger.LogWarning("Stock reservation failed for order {OrderId}: {Reason}",
                    orderEvent.OrderId, failureReason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent for order {OrderId}", orderEvent.OrderId);
            throw; 
        }
    }
}
