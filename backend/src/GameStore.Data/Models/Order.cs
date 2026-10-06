namespace GameStore.Data.Models;

public class Order
{
    public Guid Id { get; set; }

    public long OrderNumber { get; set; }

    public Guid CustomerId { get; set; }

    public DateTimeOffset Created { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? PaymentId { get; set; }

    public string? PaymentCardBrand { get; set; }

    public string? PaymentCardLast4 { get; set; }

    public DateTimeOffset LastUpdated { get; set; }

    public OrderStatus Status { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public Guid OperationId { get; set; }
}
