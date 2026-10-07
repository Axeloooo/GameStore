using GameStore.Api.Features.Orders.GetOrder;
using GameStore.Api.Features.Orders.GetOrders;

namespace GameStore.Api.Features.Orders;

public static class OrdersEndpoints
{
    public static RouteGroupBuilder MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/orders");

        group.MapGetOrder();
        group.MapGetOrders();

        return group;
    }
}
