using OrderService.Api.Domain;

namespace OrderService.Api.Infrastructure;

/// <summary>
/// Order repository interface
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task<List<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetAllPendingAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
}
