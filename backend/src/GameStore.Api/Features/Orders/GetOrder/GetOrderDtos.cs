namespace GameStore.Api.Features.Orders.GetOrder;

public record OrderDto(
    Guid Id,
    long OrderNumber,
    Guid CustomerId,
    DateTimeOffset Created,
    string Status,
    decimal? TotalAmount,
    string? PaymentCardBrand,
    string? PaymentCardLast4,
    IEnumerable<OrderItemDto> Items
);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    string ImageUri,
    IEnumerable<string> GameCodes
);
