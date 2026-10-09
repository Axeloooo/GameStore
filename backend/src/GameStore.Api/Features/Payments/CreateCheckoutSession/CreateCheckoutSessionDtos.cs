using GameStore.Api.Shared.Validations;

namespace GameStore.Api.Features.Payments.CreateCheckoutSession;

public record CreateCheckoutSessionDto([NotEmptyGuid] Guid OperationId);

public record CheckoutSessionDto(
    string ClientSecret,
    Guid OrderId,
    IEnumerable<CheckoutSessionItemDto> Items
);

public record CheckoutSessionItemDto(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    string ImageUri
);
