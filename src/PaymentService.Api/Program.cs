using MassTransit;
using PaymentService.Api.Consumers;
using PaymentService.Api.Services;
using ServiceDefaults;
using Shared.Contracts.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IPaymentProcessor, PaymentProcessor>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("rabbitmq-msg"));
        
        cfg.ReceiveEndpoint("payment-stock-reserved", e =>
        {
            e.ConfigureConsumer<StockReservedEventConsumer>(context);
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

app.MapGet("/payments/{orderId:guid}", async (Guid orderId, IPaymentProcessor paymentProcessor) =>
{
    var payment = await paymentProcessor.GetPaymentByOrderIdAsync(orderId);
    return payment != null ? Results.Ok(new PaymentDto
    {
        PaymentId = payment.PaymentId,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        Status = payment.Status.ToString(),
        ProcessedAt = payment.ProcessedAt
    }) : Results.NotFound();
});

app.Run();
