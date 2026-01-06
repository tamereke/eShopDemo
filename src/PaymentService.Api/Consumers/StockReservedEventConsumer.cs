using MassTransit;
using PaymentService.Api.Services;
using Shared.Contracts.Events;

namespace PaymentService.Api.Consumers;

public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
{
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<StockReservedEventConsumer> _logger;

    public StockReservedEventConsumer(
        IPaymentProcessor paymentProcessor,
        IPublishEndpoint publishEndpoint,
        ILogger<StockReservedEventConsumer> logger)
    {
        _paymentProcessor = paymentProcessor;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var stockEvent = context.Message;

        if (!stockEvent.IsReserved)
        {
            _logger.LogWarning("Stock reservation failed for order {OrderId}, skipping payment", stockEvent.OrderId);
            return;
        }

        _logger.LogInformation("Processing payment for order {OrderId} with amount {Amount}",
            stockEvent.OrderId, stockEvent.TotalAmount);

        try
        {
            var payment = await _paymentProcessor.ProcessPaymentAsync(stockEvent.OrderId, stockEvent.TotalAmount);

            var paymentEvent = new PaymentProcessedEvent
            {
                OrderId = stockEvent.OrderId,
                PaymentId = payment.PaymentId,
                IsSuccess = payment.Status == Models.PaymentStatus.Success,
                Amount = payment.Amount,
                FailureReason = payment.FailureReason,
                ProcessedAt = payment.ProcessedAt
            };

            await _publishEndpoint.Publish(paymentEvent);
            
            _logger.LogInformation("Payment {PaymentId} for order {OrderId} processed: {Status}",
                payment.PaymentId, stockEvent.OrderId, payment.Status);
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", stockEvent.OrderId);
            throw;
        }
    }
}
