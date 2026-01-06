using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using OrderService.Api.Application.Commands;
using OrderService.Api.Domain;
using OrderService.Api.Infrastructure;
using Shared.Contracts.DTOs;
using Shared.Contracts.Events;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OrderService.Tests.Application;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockRepository;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _mockLogger;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _mockRepository = new Mock<IOrderRepository>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var client = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };
        _mockHttpClientFactory.Setup(x => x.CreateClient("InventoryService")).Returns(client);

        _handler = new CreateOrderCommandHandler(
            _mockRepository.Object,
            _mockPublishEndpoint.Object,
            _mockCache.Object,
            _mockHttpClientFactory.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_AndPublishEvent()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = "customer-123",
            Items = new List<OrderItemDto>
            {
                new() { ProductId = "product-1", ProductName = "Laptop", Quantity = 2, UnitPrice = 1500.00m }
            }
        };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        // Mock Inventory Response (Success, Enough Stock)
        var stockResponse = new InventoryDto { ProductId = "product-1", AvailableStock = 100 };
        var jsonResponse = JsonSerializer.Serialize(stockResponse);
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(() => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be("customer-123");
        result.TotalAmount.Should().Be(3000.00m);
        result.Items.Should().HaveCount(1);

        // Check if CreateAsync is called
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);

        // Check if event is published
        _mockPublishEndpoint.Verify(
            p => p.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Kafka audit publish
        _mockPublishEndpoint.Verify(
            p => p.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<IPipe<PublishContext<OrderCreatedEvent>>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Check if cached
        _mockCache.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldCalculateTotalAmount_Correctly()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = "customer-456",
            Items = new List<OrderItemDto>
            {
                new() { ProductId = "product-1", ProductName = "Laptop", Quantity = 2, UnitPrice = 1500.00m },
                new() { ProductId = "product-2", ProductName = "Mouse", Quantity = 3, UnitPrice = 50.00m }
            }
        };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        // Mock Inventory Response (Success for ANY product)
        var stockResponse = new InventoryDto { ProductId = "generic", AvailableStock = 999 };
        var jsonResponse = JsonSerializer.Serialize(stockResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(() => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(3150.00m); // (2 * 1500) + (3 * 50)
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenStockIsInsufficient()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            CustomerId = "customer-123",
            Items = new List<OrderItemDto>
            {
                new() { ProductId = "product-1", ProductName = "Laptop", Quantity = 10, UnitPrice = 1500.00m }
            }
        };

        // Mock Inventory Response (Success, Low Stock)
        var stockResponse = new InventoryDto { ProductId = "product-1", AvailableStock = 5 }; // Asking for 10
        var jsonResponse = JsonSerializer.Serialize(stockResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        
        // Ensure repository was NOT called
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
