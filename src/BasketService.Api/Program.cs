using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ServiceDefaults;
using Shared.Contracts.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis");
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

app.MapGet("/basket/{customerId}", async (string customerId, IDistributedCache cache) =>
{
    var basketJson = await cache.GetStringAsync(customerId);
    if (string.IsNullOrEmpty(basketJson))
    {
        return Results.Ok(new BasketDto { CustomerId = customerId });
    }

    var basket = JsonSerializer.Deserialize<BasketDto>(basketJson);
    return Results.Ok(basket);
});

app.MapPost("/basket", async (BasketDto basket, IDistributedCache cache) =>
{
    var basketJson = JsonSerializer.Serialize(basket);
    await cache.SetStringAsync(basket.CustomerId, basketJson);
    return Results.Ok(basket);
});

app.MapDelete("/basket/{customerId}", async (string customerId, IDistributedCache cache) =>
{
    await cache.RemoveAsync(customerId);
    return Results.NoContent();
});

app.Run();
