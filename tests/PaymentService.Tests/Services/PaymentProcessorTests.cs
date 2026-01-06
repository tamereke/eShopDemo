using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentService.Api.Models;
using PaymentService.Api.Services;
using Xunit;

namespace PaymentService.Tests.Services;

public class PaymentProcessorTests
{
    private readonly Mock<ILogger<PaymentProcessor>> _mockLogger;
    private readonly PaymentProcessor _paymentProcessor;

    public PaymentProcessorTests()
    {
        _mockLogger = new Mock<ILogger<PaymentProcessor>>();
        _paymentProcessor = new PaymentProcessor(_mockLogger.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldReturnPayment_WithSuccessOrFailed()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = 1500.00m;

        // Act
        var payment = await _paymentProcessor.ProcessPaymentAsync(orderId, amount);

        // Assert
        payment.Should().NotBeNull();
        payment.OrderId.Should().Be(orderId);
        payment.Amount.Should().Be(amount);
        payment.Status.Should().BeOneOf(PaymentStatus.Success, PaymentStatus.Failed);
        payment.PaymentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldHaveFailureReason_WhenPaymentFails()
    {
        // Arrange & Act
        // Since it's 80% success rate, 50 attempts should hit at least one failure
        Payment? failedPayment = null;
        for (int i = 0; i < 50; i++)
        {
            var payment = await _paymentProcessor.ProcessPaymentAsync(Guid.NewGuid(), 100m);
            if (payment.Status == PaymentStatus.Failed)
            {
                failedPayment = payment;
                break;
            }
        }

        // Assert
        failedPayment.Should().NotBeNull();
        failedPayment!.FailureReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPaymentByOrderIdAsync_ShouldReturnPayment_WhenExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await _paymentProcessor.ProcessPaymentAsync(orderId, 500m);

        // Act
        var payment = await _paymentProcessor.GetPaymentByOrderIdAsync(orderId);

        // Assert
        payment.Should().NotBeNull();
        payment!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetPaymentByOrderIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var payment = await _paymentProcessor.GetPaymentByOrderIdAsync(orderId);

        // Assert
        payment.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPaymentsAsync_ShouldReturnAllProcessedPayments()
    {
        // Arrange
        var count = 5;
        for (int i = 0; i < count; i++)
        {
            await _paymentProcessor.ProcessPaymentAsync(Guid.NewGuid(), 100m + i);
        }

        // Act
        var payments = await _paymentProcessor.GetAllPaymentsAsync();

        // Assert
        payments.Should().HaveCountGreaterThanOrEqualTo(count);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldSetProcessedAt_ToCurrentTime()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        // Act
        await Task.Delay(10);
        var payment = await _paymentProcessor.ProcessPaymentAsync(orderId, 250m);
        var after = DateTime.UtcNow;

        // Assert
        payment.ProcessedAt.Should().BeAfter(before);
        payment.ProcessedAt.Should().BeBefore(after.AddSeconds(1));
    }
}
