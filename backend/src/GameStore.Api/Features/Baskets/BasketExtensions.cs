using GameStore.Api.Features.Baskets.Authorization;
using GameStore.Api.Features.Baskets.ClearBasket;
using Microsoft.AspNetCore.Authorization;

namespace GameStore.Api.Features.Baskets;

public static class BasketExtensions
{
    public static IHostApplicationBuilder AddBasketServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorizationHandler, BasketAuthorizationHandler>();
        builder.Services.AddScoped<BasketItemsProvider>()
                        .AddScoped<ClearBasketOperation>();

        return builder;
    }
}
