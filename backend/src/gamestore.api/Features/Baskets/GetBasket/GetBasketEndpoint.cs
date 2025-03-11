using gamestore.api.Data;
using gamestore.api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamestore.api.Features.Baskets.GetBasket;

public static class GetBasketEndpoint
{
    public static void MapGetBasket(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/{userId}",
            async (Guid userId, GameStoreContext dbContext) =>
            {
                if (userId == Guid.Empty)
                {
                    return Results.BadRequest();
                }

                CustomerBasket basket =
                    await dbContext
                        .Baskets.Include(basket => basket.Items)
                        .ThenInclude(item => item.Game)
                        .FirstOrDefaultAsync(basket => basket.Id == userId)
                    ?? new() { Id = userId };

                var dto = new BasketDto(
                    basket.Id,
                    basket
                        .Items.Select(item => new BasketItemDto(
                            item.GameId,
                            item.Game!.Name,
                            item.Game!.Price,
                            item.Quantity,
                            item.Game!.ImageUri
                        ))
                        .OrderBy(item => item.Name)
                );

                return Results.Ok(dto);
            }
        );
    }
}
