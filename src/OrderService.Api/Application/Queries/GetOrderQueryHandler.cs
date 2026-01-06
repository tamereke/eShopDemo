using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using OrderService.Api.Infrastructure;
using Shared.Contracts.DTOs;
using System.Text.Json;

namespace OrderService.Api.Application.Queries;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetOrderQueryHandler> _logger;

    public GetOrderQueryHandler(
        IOrderRepository orderRepository,
        IDistributedCache cache,
        ILogger<GetOrderQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"order:{request.OrderId}";

        var cachedOrder = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedOrder))
        {
            _logger.LogDebug("Order found in cache: {OrderId}", request.OrderId);
            return JsonSerializer.Deserialize<OrderDto>(cachedOrder);
        }

        _logger.LogDebug("Order not in cache, querying database: {OrderId}", request.OrderId);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order not found: {OrderId}", request.OrderId);
            return null;
        }

        var orderDto = new OrderDto
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

        var serialized = JsonSerializer.Serialize(orderDto);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);

        return orderDto;
    }
}
