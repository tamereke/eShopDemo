namespace PaymentService.Api.Models;

public class Payment
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failed
}
