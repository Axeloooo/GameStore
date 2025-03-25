using System.Security.Claims;
using gamestore.api.Data;
using gamestore.api.Features.Baskets.Authorization;
using gamestore.api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace gamestore.api.Features.Baskets.UpsertBasket;

public static class UpsertBasketEndpoint
{
    public static void MapUpsertBasket(this IEndpointRouteBuilder app)
    {
        app.MapPut(
            "/{userId}",
            async (
                Guid userId,
                UpsertBasketDto upsertBasketDto,
                GameStoreContext dbContext,
                IAuthorizationService authService,
                ClaimsPrincipal user
            ) =>
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

                AuthorizationResult authResult = await authService.AuthorizeAsync(
                    user,
                    basket,
                    new OwnerOrAdminRequirement()
                );

                if (!authResult.Succeeded)
                {
                    return Results.Forbid();
                }

                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            }
        );
    }
}
