using MediatR;
using Shared.Contracts.DTOs;

namespace OrderService.Api.Application.Commands;

public record CreateOrderCommand : IRequest<OrderDto>
{
    public string CustomerId { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}
