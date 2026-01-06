using ServiceDefaults;
using Shared.Contracts.DTOs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient("CatalogService", client => client.BaseAddress = new Uri("http://catalogservice"));
builder.Services.AddHttpClient("OrderService", client => client.BaseAddress = new Uri("http://orderservice"));
builder.Services.AddHttpClient("InventoryService", client => client.BaseAddress = new Uri("http://inventoryservice"));
builder.Services.AddHttpClient("PaymentService", client => client.BaseAddress = new Uri("http://paymentservice"));
builder.Services.AddHttpClient("BasketService", client => client.BaseAddress = new Uri("http://basketservice"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapDefaultEndpoints();

app.MapHub<Gateway.Api.Hubs.EventHub>("/eventhub");

app.MapPost("/api/monitor/send", async (Shared.Contracts.DTOs.LogMessageDto log, Microsoft.AspNetCore.SignalR.IHubContext<Gateway.Api.Hubs.EventHub> hub, ILogger<Program> logger) =>
{
    logger.LogInformation("Broadcasting log: {Source} - {Message}", log.Source, log.Message);
    await hub.Clients.All.SendAsync("ReceiveLog", log);
    return Results.Ok();
});

app.MapGet("/api/products", async (IHttpClientFactory factory) =>
{
    var catalogClient = factory.CreateClient("CatalogService");
    var inventoryClient = factory.CreateClient("InventoryService");

    var products = await catalogClient.GetFromJsonAsync<List<ProductDto>>("/products");
    var inventories = await inventoryClient.GetFromJsonAsync<List<InventoryDto>>("/inventory");

    var result = products?.Select(p => new
    {
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        p.ImageUri,
        p.CategoryId,
        p.CategoryName,
        Stock = inventories?.FirstOrDefault(i => i.ProductId == p.Id)?.AvailableStock ?? 0
    });

    return Results.Ok(result);
});

app.MapPost("/api/orders", async (CreateOrderRequest request, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    var response = await client.PostAsJsonAsync("/orders", request);
    return response.IsSuccessStatusCode ? Results.Created($"/api/orders", await response.Content.ReadFromJsonAsync<OrderDto>()) : Results.Problem();
});

app.MapGet("/api/orders/{id:guid}", async (Guid id, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    return await client.GetAsync($"/orders/{id}") is var r && r.IsSuccessStatusCode ? Results.Ok(await r.Content.ReadFromJsonAsync<OrderDto>()) : Results.NotFound();
});

app.MapGet("/api/orders/customer/{customerId}", async (string customerId, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    var response = await client.GetAsync($"/orders/customer/{customerId}");
    return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<List<OrderDto>>()) : Results.Problem();
});

app.MapGet("/api/orders/pending", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    var response = await client.GetAsync("/orders/pending");
    return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<List<OrderDto>>()) : Results.Problem();
});

app.MapPut("/api/orders/{id:guid}/approve", async (Guid id, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    var response = await client.PutAsync($"/orders/{id}/approve", null);
    return response.IsSuccessStatusCode ? Results.Ok() : Results.Problem();
});

app.MapPut("/api/orders/{id:guid}/cancel", async (Guid id, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    var response = await client.PutAsync($"/orders/{id}/cancel", null);
    return response.IsSuccessStatusCode ? Results.Ok() : Results.Problem();
});

app.MapGet("/api/orders/status/{status}", async (string status, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("OrderService");
    var response = await client.GetAsync($"/orders/status/{status}");
    return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<List<OrderDto>>()) : Results.Problem();
});

app.MapGet("/api/basket/{customerId}", async (string customerId, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("BasketService");
    return await client.GetFromJsonAsync<BasketDto>($"/basket/{customerId}");
});

app.MapPost("/api/basket", async (BasketDto request, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("BasketService");
    var response = await client.PostAsJsonAsync("/basket", request);
    return response.IsSuccessStatusCode ? Results.Ok(await response.Content.ReadFromJsonAsync<BasketDto>()) : Results.Problem();
});

app.MapDelete("/api/basket/{customerId}", async (string customerId, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("BasketService");
    var response = await client.DeleteAsync($"/basket/{customerId}");
    return response.IsSuccessStatusCode ? Results.NoContent() : Results.Problem();
});

app.MapGet("/api/categories", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("CatalogService");
    return await client.GetFromJsonAsync<List<CategoryDto>>("/categories");
});

app.MapPost("/api/categories", async (CreateCategoryRequest request, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("CatalogService");
    var response = await client.PostAsJsonAsync("/categories", request);
    return response.IsSuccessStatusCode ? Results.Created($"/api/categories", await response.Content.ReadFromJsonAsync<CategoryDto>()) : Results.Problem();
});


app.MapPost("/api/products", async (CreateProductRequest request, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("CatalogService");
    var response = await client.PostAsJsonAsync("/products", request);
    return response.IsSuccessStatusCode ? Results.Created($"/api/products", await response.Content.ReadFromJsonAsync<ProductDto>()) : Results.Problem();
});

app.MapPut("/api/products/{id}", async (string id, UpdateProductRequest request, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("CatalogService");
    var response = await client.PutAsJsonAsync($"/products/{id}", request);
    return response.IsSuccessStatusCode ? Results.NoContent() : Results.Problem();
});

app.MapDelete("/api/products/{id}", async (string id, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("CatalogService");
    var response = await client.DeleteAsync($"/products/{id}");
    return response.IsSuccessStatusCode ? Results.NoContent() : Results.Problem();
});

app.Run();
