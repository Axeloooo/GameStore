using System.Security.Claims;
using GameStore.Data;
using GameStore.Api.Shared.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Orders.GetOrder;

public static class GetOrderEndpoint
{
    public static void MapGetOrder(this IEndpointRouteBuilder app)
    {
        // GET /orders/{id}
        app.MapGet("/{id}", async (
            Guid id,
            GameStoreContext dbContext,
            ClaimsPrincipal user
        ) =>
        {
            var userIdClaimValue = user.FindFirstValue(GameStoreClaimTypes.UserId);

            if (!Guid.TryParse(userIdClaimValue, out var userId))
            {
                return Results.Forbid();
            }

            var order = await dbContext.Orders
                                .Include(order => order.Items)
                                .FirstOrDefaultAsync(order => order.Id == id);

            if (order is null)
            {
                return Results.NotFound();
            }

            if (userId != order.CustomerId)
            {
                return Results.Forbid();
            }

            var dto = new OrderDto(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.Created,
                order.Status.ToString(),
                order.TotalAmount,
                order.PaymentCardBrand,
                order.PaymentCardLast4,
                order.Items.Select(item => new OrderItemDto(
                    item.ProductId,
                    item.ProductName,
                    item.Price,
                    item.Quantity,
                    item.ImageUri,
                    item.GameCodes
                ))
            );

            return Results.Ok(dto);
        });
    }
}
