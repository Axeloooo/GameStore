using GameStore.Contracts.Orders;
using GameStore.Data;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Worker.MessageHandlers.Orders;

public class OrderPaidHandler(
    GameStoreContext context,
    TimeProvider timeProvider,
    ILogger<OrderPaidHandler> logger
) : IMessageHandler<OrderPaid>
{
    public async Task HandleAsync(OrderPaid orderPaid, CancellationToken ct = default)
    {
        if (orderPaid is null || orderPaid.OrderId == Guid.Empty)
        {
            logger.LogWarning("Ignoring invalid OrderPaid message.");
            return;
        }

        var orderId = orderPaid.OrderId;

        logger.LogInformation("Processing OrderPaid for Order {OrderId}", orderId);

        var order = await context.Orders
                                .Include(order => order.Items)
                                .FirstOrDefaultAsync(
                                    order => order.Id == orderId, ct);

        if (order is null)
        {
            logger.LogError("Order not found.");
            return;
        }

        if (order.Status != OrderStatus.Processing)
        {
            logger.LogWarning("Order {OrderId} is not in Processing status. Status: {Status}",
                                order.Id,
                                order.Status);
            return;
        }

        foreach (var item in order.Items)
        {
            item.GameCodes = GameCodeGenerator.GenerateCodes(item.Quantity);
        }

        order.Status = OrderStatus.Completed;
        order.LastUpdated = timeProvider.GetUtcNow();

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully assigned game codes for order {OrderId}",
                              orderId);
    }
}
