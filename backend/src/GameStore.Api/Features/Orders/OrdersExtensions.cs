using GameStore.Api.Features.Orders.ConfirmOrderPayment;
using GameStore.Api.Features.Orders.CreateOrder;

namespace GameStore.Api.Features.Orders;

public static class OrdersExtensions
{
    public static IHostApplicationBuilder AddOrderServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<OrderCreator>()
                        .AddScoped<ConfirmOrderPaymentOperation>();

        return builder;
    }
}
