using System.Text.Json;
using GameStore.Data;
using GameStore.Api.Features.Baskets.ClearBasket;
using GameStore.Api.Features.Orders.Constants;
using GameStore.Data.Models;
using GameStore.Api.Shared.Messaging;
using GameStore.Contracts.Orders;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Orders.ConfirmOrderPayment;

public class ConfirmOrderPaymentOperation(
    GameStoreContext context,
    TimeProvider timeProvider,
    ClearBasketOperation clearBasketOperation,
    ILogger<ConfirmOrderPaymentOperation> logger
)
{
    public async Task<bool> ExecuteAsync(
        Guid orderId,
        string paymentId,
        string cardBrand,
        string cardLast4,
        decimal amountCharged
    )
    {
        if (orderId == Guid.Empty)
        {
            logger.LogError("Invalid empty orderId provided.");
            return false;
        }

        var order = await context.Orders.FindAsync(orderId);

        if (order is null)
        {
            logger.LogError("Order {OrderId} not found", orderId);
            return false;
        }

        if (order.Status == OrderStatus.Processing
            || order.Status == OrderStatus.Completed)
        {
            logger.LogInformation("Order {OrderId} already processed with status {Status}",
                                    orderId,
                                    order.Status);
            return true;
        }

        if (order.Status != OrderStatus.Pending)
        {
            logger.LogError("Order {OrderId} has invalid status {Status}.",
                            orderId,
                            order.Status);
            return false;
        }

        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var orderToUpdate = await context.Orders.FindAsync(orderId);

            if (orderToUpdate is null || orderToUpdate.Status != OrderStatus.Pending)
            {
                logger.LogWarning("Order {OrderId} not found or not in pending status",
                                    orderId);
                return;
            }

            orderToUpdate.Status = OrderStatus.Processing;
            orderToUpdate.PaymentId = paymentId;
            orderToUpdate.PaymentCardBrand = cardBrand;
            orderToUpdate.PaymentCardLast4 = cardLast4;
            orderToUpdate.TotalAmount = amountCharged;
            orderToUpdate.LastUpdated = timeProvider.GetUtcNow();

            await context.SaveChangesAsync();

            await clearBasketOperation.ExecuteAsync(orderToUpdate.CustomerId);

            var message = new OrderPaid(orderId);

            var outboxMessage = new OutboxMessage
            {
                MessageType = message.GetType().Name,
                QueueName = QueueNames.Orders,
                Payload = JsonSerializer.Serialize(message),
                MessageId = paymentId,
                CorrelationId = orderId.ToString(),
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime
            };

            context.OutboxMessages.Add(outboxMessage);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            logger.LogInformation("Successfully updated order {OrderId} to Processing status and cleared the basket for user {CustomerId}.",
                                    orderId,
                                    orderToUpdate.CustomerId);
        });

        return true;
    }
}
