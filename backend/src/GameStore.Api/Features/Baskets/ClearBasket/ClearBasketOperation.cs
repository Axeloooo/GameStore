using GameStore.Data;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Baskets.ClearBasket;

public class ClearBasketOperation(
    GameStoreContext context,
    ILogger<ClearBasketOperation> logger
)
{
    public async Task ExecuteAsync(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
        }

        await context.Baskets
                    .Where(basket => basket.Id == customerId)
                    .ExecuteDeleteAsync();

        logger.LogInformation("Cleared basket for customer {CustomerId}",
                                customerId);
    }
}
