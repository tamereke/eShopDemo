using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Domain;
using OrderService.Api.Infrastructure;
using Xunit;

namespace OrderService.Tests.Infrastructure;

public class OrderRepositoryTests
{
    private readonly DbContextOptions<OrderDbContext> _options;

    public OrderRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetAllPendingAsync_ShouldReturnOnlyPendingOrders()
    {
        // Arrange
        using (var context = new OrderDbContext(_options))
        {
            context.Orders.Add(new Order("customer-1")); // Pending by default
            
            var confirmedOrder = new Order("customer-2");
            confirmedOrder.Confirm();
            context.Orders.Add(confirmedOrder);
            
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new OrderDbContext(_options))
        {
            var repository = new OrderRepository(context);
            var result = await repository.GetAllPendingAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().CustomerId.Should().Be("customer-1");
            result.First().Status.Should().Be(OrderStatus.Pending);
        }
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnOrdersWithSpecifiedStatus()
    {
        // Arrange
        using (var context = new OrderDbContext(_options))
        {
            var order1 = new Order("customer-1");
            order1.Cancel("test");
            context.Orders.Add(order1);

            var order2 = new Order("customer-2"); // Pending
            context.Orders.Add(order2);

            var order3 = new Order("customer-3");
            order3.Cancel("test2");
            context.Orders.Add(order3);

            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new OrderDbContext(_options))
        {
            var repository = new OrderRepository(context);
            var result = await repository.GetByStatusAsync(OrderStatus.Cancelled);

            // Assert
            result.Should().HaveCount(2);
            result.All(o => o.Status == OrderStatus.Cancelled).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnOrdersForCustomer()
    {
        // Arrange
        using (var context = new OrderDbContext(_options))
        {
            context.Orders.Add(new Order("customer-1"));
            context.Orders.Add(new Order("customer-1"));
            context.Orders.Add(new Order("customer-2"));
            await context.SaveChangesAsync();
        }

        // Act
        using (var context = new OrderDbContext(_options))
        {
            var repository = new OrderRepository(context);
            var result = await repository.GetByCustomerIdAsync("customer-1");

            // Assert
            result.Should().HaveCount(2);
            result.All(o => o.CustomerId == "customer-1").Should().BeTrue();
        }
    }
}
