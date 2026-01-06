using InventoryService.Api.Consumers;
using InventoryService.Api.Services;
using MassTransit;
using ServiceDefaults;
using Shared.Contracts.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IInventoryService, InventoryService.Api.Services.InventoryService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq-msg"));
        
        cfg.ReceiveEndpoint("inventory-order-created", e =>
        {
            e.ConfigureConsumer<OrderCreatedEventConsumer>(context);
        });
    });

    x.AddRider(rider =>
    {
        rider.UsingKafka((context, k) =>
        {
            k.Host(builder.Configuration.GetConnectionString("kafka"));
        });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapGet("/inventory/{productId}", async (string productId, IInventoryService inventoryService) =>
{
    var product = await inventoryService.GetProductAsync(productId);
    return product != null ? Results.Ok(new InventoryDto
    {
        ProductId = product.ProductId,
        ProductName = product.ProductName,
        AvailableStock = product.AvailableStock,
        ReservedStock = product.ReservedStock
    }) : Results.NotFound();
});

app.MapGet("/inventory", async (IInventoryService inventoryService) =>
{
    var products = await inventoryService.GetAllProductsAsync();
    return Results.Ok(products.Select(p => new InventoryDto
    {
        ProductId = p.ProductId,
        ProductName = p.ProductName,
        AvailableStock = p.AvailableStock,
        ReservedStock = p.ReservedStock
    }));
});

app.Run();
