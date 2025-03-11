namespace gamestore.api.Features.Baskets.UpsertBasket;

public record class UpsertBasketDto(IEnumerable<UpsertBasketItemDto> items);

public record class UpsertBasketItemDto(Guid Id, int Quantity);
