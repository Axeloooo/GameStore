using System.Security.Claims;
using GameStore.Data;
using GameStore.Data.Models;
using GameStore.Api.Shared.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Orders.GetOrders;

public static class GetOrdersEndpoint
{
    public static void MapGetOrders(this IEndpointRouteBuilder app)
    {
        // GET /orders
        app.MapGet("/", async (
            [AsParameters] GetOrdersDto request,
            GameStoreContext dbContext,
            ClaimsPrincipal user
        ) =>
        {
            var userIdClaimValue = user.FindFirstValue(GameStoreClaimTypes.UserId);

            if (!Guid.TryParse(userIdClaimValue, out var userId))
            {
                return Results.Forbid();
            }

            var filteredOrders = dbContext.Orders
                                .Where(order => order.CustomerId == userId
                                        && order.Status != OrderStatus.Pending);

            var skipCount = (request.PageNumber - 1) * request.PageSize;

            var ordersOnPage = await filteredOrders
                                .OrderByDescending(order => order.Created)
                                .Skip(skipCount)
                                .Take(request.PageSize)
                                .Include(order => order.Items)
                                .Select(order => new OrderDto(
                                    order.Id,
                                    order.OrderNumber,
                                    order.CustomerId,
                                    order.Created,
                                    order.Status.ToString(),
                                    order.TotalAmount,
                                    order.Items.Select(item => new OrderItemDto(
                                        item.ProductId,
                                        item.ProductName,
                                        item.Price,
                                        item.Quantity,
                                        item.ImageUri
                                    ))
                                ))
                                .AsNoTracking()
                                .ToListAsync();

            var totalOrders = await filteredOrders.CountAsync();
            var totalPages = (int)Math.Ceiling(totalOrders / (double)request.PageSize);

            return Results.Ok(new OrdersPageDto(totalPages, ordersOnPage));
        });
    }
}
