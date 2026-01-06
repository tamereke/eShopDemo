using FluentAssertions;
using OrderService.Api.Domain;
using Xunit;

namespace OrderService.Tests.Domain;

public class OrderTests
{
    [Fact]
    public void Order_ShouldBeCreated_WithValidCustomerId()
    {
        // Arrange
        var customerId = "customer-123";

        // Act
        var order = new Order(customerId);

        // Assert
        order.Should().NotBeNull();
        order.CustomerId.Should().Be(customerId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().BeEmpty();
        order.TotalAmount.Should().Be(0);
    }

    [Fact]
    public void Order_ShouldThrowException_WhenCustomerIdIsEmpty()
    {
        // Arrange & Act & Assert
        Action act = () => new Order(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CustomerId*");
    }

    [Fact]
    public void AddItem_ShouldAddNewItem_WhenProductDoesNotExist()
    {
        // Arrange
        var order = new Order("customer-123");

        // Act
        order.AddItem("product-1", "Laptop", 2, 1500.00m);

        // Assert
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(3000.00m);
        var item = order.Items.First();
        item.ProductId.Should().Be("product-1");
        item.Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_ShouldIncreaseQuantity_WhenProductAlreadyExists()
    {
        // Arrange
        var order = new Order("customer-123");
        order.AddItem("product-1", "Laptop", 2, 1500.00m);

        // Act
        order.AddItem("product-1", "Laptop", 3, 1500.00m);

        // Assert
        order.Items.Should().HaveCount(1);
        var item = order.Items.First();
        item.Quantity.Should().Be(5); // 2 + 3
        order.TotalAmount.Should().Be(7500.00m); // 5 * 1500
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_ShouldThrowException_WhenQuantityIsInvalid(int quantity)
    {
        // Arrange
        var order = new Order("customer-123");

        // Act & Assert
        Action act = () => order.AddItem("product-1", "Laptop", quantity, 1500.00m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity*");
    }

    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmed_WhenOrderIsPending()
    {
        // Arrange
        var order = new Order("customer-123");
        order.AddItem("product-1", "Laptop", 1, 1500.00m);

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_ShouldThrowException_WhenOrderIsNotPending()
    {
        // Arrange
        var order = new Order("customer-123");
        order.Confirm();

        // Act & Assert
        Action act = () => order.Confirm();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled_WhenOrderIsPending()
    {
        // Arrange
        var order = new Order("customer-123");

        // Act
        order.Cancel("Customer requested");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void Cancel_ShouldThrowException_WhenOrderIsInFinalState(OrderStatus status)
    {
        // Arrange
        var order = new Order("customer-123");
        order.UpdateStatus(status);

        // Act & Assert
        Action act = () => order.Cancel("Test");
        act.Should().Throw<InvalidOperationException>();
    }
}
