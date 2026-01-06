using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Application.Commands;
using OrderService.Api.Application.Queries;
using OrderService.Api.Domain;
using OrderService.Api.Infrastructure;
using ServiceDefaults;
using Shared.Contracts.DTOs;
using Shared.Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
});

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq-msg"));
        cfg.ConfigureEndpoints(context);
    });

    x.AddRider(rider =>
    {
        rider.AddProducer<OrderConfirmedEvent>("order-confirmed");
        rider.AddProducer<OrderCancelledEvent>("order-cancelled");
        
        rider.UsingKafka((context, k) =>
        {
            k.Host(builder.Configuration.GetConnectionString("kafka"));
        });
    });
});

builder.Services.AddHttpClient("InventoryService", client =>
{
    client.BaseAddress = new Uri("http://inventoryservice");
});

builder.Services.AddHttpClient("Gateway", client => client.BaseAddress = new Uri("http://gateway"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

for (int i = 0; i < 5; i++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        break; 
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database not ready yet, retrying... (Attempt {Attempt})", i + 1);
        await Task.Delay(5000 * (i + 1)); 
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator) =>
{
    try 
    {
        var command = new CreateOrderCommand { CustomerId = request.CustomerId, Items = request.Items };
        var result = await mediator.Send(command);
        return Results.Created($"/orders/{result.OrderId}", result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var query = new GetOrderQuery { OrderId = id };
    var result = await mediator.Send(query);
    return result != null ? Results.Ok(result) : Results.NotFound();
});

app.MapGet("/orders/customer/{customerId}", async (string customerId, IOrderRepository repository) =>
{
    var orders = await repository.GetByCustomerIdAsync(customerId);
    return Results.Ok(orders.Select(o => new OrderDto
    {
        OrderId = o.Id,
        CustomerId = o.CustomerId,
        CreatedAt = o.CreatedAt,
        Status = o.Status.ToString(),
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    }));
});

app.MapGet("/orders/pending", async (IOrderRepository repository) =>
{
    var orders = await repository.GetAllPendingAsync();
    return Results.Ok(orders.Select(o => new OrderDto
    {
        OrderId = o.Id,
        CustomerId = o.CustomerId,
        CreatedAt = o.CreatedAt,
        Status = o.Status.ToString(),
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    }));
});

app.MapPut("/orders/{id:guid}/approve", async (Guid id, IOrderRepository repository, IPublishEndpoint publishEndpoint) =>
{
    var order = await repository.GetByIdAsync(id);
    if (order == null) return Results.NotFound();

    try
    {
        order.Confirm();
        await repository.UpdateAsync(order);

        var confirmedEvent = new OrderConfirmedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            ConfirmedAt = DateTime.UtcNow
        };
        
        app.Logger.LogInformation("Publishing OrderConfirmedEvent for {OrderId}", order.Id);

        await publishEndpoint.Publish(confirmedEvent);
         
        var producer = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITopicProducer<OrderConfirmedEvent>>();
        await producer.Produce(confirmedEvent);

        return Results.Ok();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapGet("/orders/status/{status}", async (string status, IOrderRepository repository) =>
{
    if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
    {
        var orders = await repository.GetByStatusAsync(orderStatus);
        return Results.Ok(orders.Select(o => new OrderDto
        {
            OrderId = o.Id,
            CustomerId = o.CustomerId,
            CreatedAt = o.CreatedAt,
            Status = o.Status.ToString(),
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        }));
    }
    
    return Results.BadRequest("Invalid status");
});

app.MapPut("/orders/{id:guid}/cancel", async (Guid id, IOrderRepository repository, IPublishEndpoint publishEndpoint) =>
{
    var order = await repository.GetByIdAsync(id);
    if (order == null) return Results.NotFound();

    try
    {
        string reason = "Cancelled by user/admin";
        order.Cancel(reason);
        await repository.UpdateAsync(order);

        var cancelledEvent = new OrderCancelledEvent
        {
            OrderId = order.Id,
            Reason = reason,
            CancelledAt = DateTime.UtcNow
        };

        await publishEndpoint.Publish(cancelledEvent);
        
        var producer = app.Services.CreateScope().ServiceProvider.GetRequiredService<ITopicProducer<OrderCancelledEvent>>();
        await producer.Produce(cancelledEvent);

        return Results.Ok();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();
