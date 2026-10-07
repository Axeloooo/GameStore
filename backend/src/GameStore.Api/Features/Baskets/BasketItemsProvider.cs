using GameStore.Data;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Baskets;

public class BasketItemsProvider(GameStoreContext dbContext)
{
    public async Task<IReadOnlyList<BasketItem>> GetBasketItemsAsync(Guid userId)
    {
        var basket = await dbContext.Baskets
                            .Include(basket => basket.Items)
                            .ThenInclude(item => item.Game)
                            .FirstOrDefaultAsync(basket => basket.Id == userId);

        if (basket == null)
        {
            return [];
        }

        return basket.Items.AsReadOnly();
    }
}
