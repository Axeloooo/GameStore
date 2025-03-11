using gamestore.api.Data;
using gamestore.api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamestore.api.Features.Baskets.UpsertBasket;

public static class UpsertBasketEndpoint
{
    public static void MapUpsertBasket(this IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/{userId}",
            async (Guid userId, UpsertBasketDto upsertBasketDto, GameStoreContext dbContext) =>
            {
                CustomerBasket? basket = await dbContext
                    .Baskets.Include(basket => basket.Items)
                    .FirstOrDefaultAsync(basket => basket.Id == userId);

                if (basket is null)
                {
                    basket = new CustomerBasket
                    {
                        Id = userId,
                        Items = upsertBasketDto
                            .items.Select(item => new BasketItem
                            {
                                GameId = item.Id,
                                Quantity = item.Quantity,
                            })
                            .ToList(),
                    };
                    dbContext.Baskets.Add(basket);
                }
                else
                {
                    basket.Items = upsertBasketDto
                        .items.Select(item => new BasketItem
                        {
                            GameId = item.Id,
                            Quantity = item.Quantity,
                        })
                        .ToList();
                }

                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            }
        );
    }
}
