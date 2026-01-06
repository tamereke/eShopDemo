using MediatR;
using Shared.Contracts.DTOs;

namespace OrderService.Api.Application.Queries;

public record GetOrderQuery : IRequest<OrderDto?>
{
    public Guid OrderId { get; init; }
}
