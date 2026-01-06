using PaymentService.Api.Models;
using System.Collections.Concurrent;

namespace PaymentService.Api.Services;

public class PaymentProcessor : IPaymentProcessor
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private readonly ILogger<PaymentProcessor> _logger;
    private readonly Random _random = new();

    public PaymentProcessor(ILogger<PaymentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<Payment> ProcessPaymentAsync(Guid orderId, decimal amount)
    {
        _logger.LogInformation("Processing payment for order {OrderId}, amount: {Amount}", orderId, amount);

        // Simulated processing
        await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(100, 500)));

        var isSuccess = _random.NextDouble() > 0.2; 

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Status = isSuccess ? PaymentStatus.Success : PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            FailureReason = isSuccess ? null : "Insufficient funds or card declined (simulated)"
        };

        _payments.TryAdd(payment.PaymentId, payment);

        _logger.LogInformation("Payment {PaymentId} for order {OrderId} processed with status: {Status}",
            payment.PaymentId, orderId, payment.Status);

        return payment;
    }

    public Task<Payment?> GetPaymentByOrderIdAsync(Guid orderId)
    {
        var payment = _payments.Values.FirstOrDefault(p => p.OrderId == orderId);
        return Task.FromResult(payment);
    }

    public Task<IEnumerable<Payment>> GetAllPaymentsAsync()
    {
        return Task.FromResult(_payments.Values.AsEnumerable());
    }
}

public interface IPaymentProcessor
{
    Task<Payment> ProcessPaymentAsync(Guid orderId, decimal amount);
    Task<Payment?> GetPaymentByOrderIdAsync(Guid orderId);
    Task<IEnumerable<Payment>> GetAllPaymentsAsync();
}
