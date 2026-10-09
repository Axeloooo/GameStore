namespace GameStore.Data.Models;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public required string ProductName { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public required string ImageUri { get; set; }

    public List<string> GameCodes { get; set; } = [];
}
