namespace InventoryService.Api.Models;

public class InventoryItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int AvailableStock { get; set; }
    public int ReservedStock { get; set; }

    public bool CanReserve(int quantity)
    {
        return AvailableStock >= quantity;
    }

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new InvalidOperationException($"Insufficient stock for product {ProductId}");

        AvailableStock -= quantity;
        ReservedStock += quantity;
    }

    public void ReleaseReservation(int quantity)
    {
        if (ReservedStock < quantity)
            throw new InvalidOperationException($"Cannot release more than reserved for product {ProductId}");

        ReservedStock -= quantity;
        AvailableStock += quantity;
    }
}
