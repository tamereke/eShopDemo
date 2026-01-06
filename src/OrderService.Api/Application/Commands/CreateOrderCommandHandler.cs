using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using OrderService.Api.Domain;
using OrderService.Api.Infrastructure;
using Shared.Contracts.DTOs;
using Shared.Contracts.Events;
using System.Text.Json;

namespace OrderService.Api.Application.Commands;

/// <summary>
/// CreateOrderCommand handler - business logic
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IPublishEndpoint publishEndpoint,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _publishEndpoint = publishEndpoint;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for customer: {CustomerId}", request.CustomerId);

        var inventoryClient = _httpClientFactory.CreateClient("InventoryService");
        foreach (var item in request.Items)
        {
            try
            {
                var stockResponse = await inventoryClient.GetAsync($"/inventory/{item.ProductId}", cancellationToken);
                if (stockResponse.IsSuccessStatusCode)
                {
                    var stockData = await stockResponse.Content.ReadFromJsonAsync<InventoryDto>(cancellationToken: cancellationToken);
                    if (stockData != null && stockData.AvailableStock < item.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product {item.ProductName}. Available: {stockData.AvailableStock}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Could not check stock for product {ProductId}. Proceeding anyway.", item.ProductId);
                 throw new InvalidOperationException($"Cannot verify stock for {item.ProductName}. Please try again later.");
            }
        }

        var order = new Order(request.CustomerId);

        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
        }

        await _orderRepository.CreateAsync(order, cancellationToken);

        _logger.LogInformation("Order created with ID: {OrderId}", order.Id);

        var orderDto = MapToDto(order);

        await CacheOrderAsync(orderDto, cancellationToken);

        // Publish OrderCreated event to RabbitMQ
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Items = request.Items,
            OrderDate = order.CreatedAt
        };

        await _publishEndpoint.Publish(orderCreatedEvent, cancellationToken);
        _logger.LogInformation("OrderCreatedEvent published for order: {OrderId}", order.Id);

        try
        {
            var gatewayClient = _httpClientFactory.CreateClient("Gateway");
            var monitorLog = new LogMessageDto("OrderService", $"Order {order.Id} Created", "success", DateTime.UtcNow);
            gatewayClient.PostAsJsonAsync("/api/monitor/send", monitorLog, cancellationToken).ConfigureAwait(false);
        }
        catch { }

        return orderDto;
    }

    private async Task CacheOrderAsync(OrderDto orderDto, CancellationToken cancellationToken)
    {
        var cacheKey = $"order:{orderDto.OrderId}";
        var serialized = JsonSerializer.Serialize(orderDto);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
        _logger.LogDebug("Order cached: {OrderId}", orderDto.OrderId);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }
}
