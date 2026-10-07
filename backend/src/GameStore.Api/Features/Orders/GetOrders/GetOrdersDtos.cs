namespace GameStore.Api.Features.Orders.GetOrders;

public record GetOrdersDto(int PageNumber = 1, int PageSize = 5);

public record OrdersPageDto(int TotalPages, IEnumerable<OrderDto> Data);

public record OrderDto(
    Guid Id,
    long OrderNumber,
    Guid CustomerId,
    DateTimeOffset Created,
    string Status,
    decimal? TotalAmount,
    IEnumerable<OrderItemDto> Items
);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    string ImageUri
);
