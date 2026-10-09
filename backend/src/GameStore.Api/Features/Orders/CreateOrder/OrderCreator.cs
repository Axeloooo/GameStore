using GameStore.Data;
using GameStore.Api.Features.Baskets;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameStore.Api.Features.Orders.CreateOrder;

public class OrderCreator(
    GameStoreContext dbContext,
    BasketItemsProvider basketItemsProvider,
    TimeProvider timeProvider,
    ILogger<OrderCreator> logger
)
{
    public async Task<CreateOrderResult> GetOrCreateOrderAsync(
        Guid userId,
        Guid operationId)
    {
        var existingOrder = await dbContext.Orders
                            .Include(order => order.Items)
                            .FirstOrDefaultAsync(order => order.OperationId == operationId
                                                    && order.CustomerId == userId);

        if (existingOrder != null)
        {
            return new CreateOrderResult(existingOrder);
        }

        var basketItems = await basketItemsProvider.GetBasketItemsAsync(userId);

        if (!basketItems.Any())
        {
            return new CreateOrderResult(EmptyBasket: true);
        }

        var now = timeProvider.GetUtcNow();

        var order = new Order
        {
            CustomerId = userId,
            Created = now,
            Status = OrderStatus.Pending,
            LastUpdated = now,
            Items = [.. basketItems.Select(basketItem => new OrderItem{
                ProductId = basketItem.GameId,
                ProductName = basketItem.Game!.Name,
                Quantity = basketItem.Quantity,
                Price = basketItem.Game!.Price,
                ImageUri = basketItem.Game!.ImageUri
            })],
            OperationId = operationId
        };

        dbContext.Orders.Add(order);

        try
        {
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Created new order {OrderId} for user {UserId} and OperationId {OperationId}",
                                  order.Id,
                                  userId,
                                  operationId);

            return new CreateOrderResult(order);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pgEx &&
            pgEx.SqlState == PostgresErrorCodes.UniqueViolation
        )
        {
            dbContext.Entry(order).State = EntityState.Detached;

            logger.LogWarning(ex, "Database update failed for OperationId {OperationId} and UserId {UserId}. Checking for existing order.",
                                    operationId,
                                    userId);

            var raceConditionOrder = await dbContext.Orders
                                .Include(order => order.Items)
                                .FirstOrDefaultAsync(order => order.OperationId == operationId
                                                        && order.CustomerId == userId);

            if (raceConditionOrder != null)
            {
                logger.LogInformation("Found existing order {OrderId} for OperationId {OperationId}",
                                        raceConditionOrder.Id,
                                        operationId);
                return new CreateOrderResult(raceConditionOrder);
            }

            logger.LogError(ex, "No existing order found after DbUpdateException for OperationId {OperationId}",
                                operationId);
            throw;
        }
    }
}
