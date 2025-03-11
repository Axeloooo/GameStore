using gamestore.api.Features.Baskets.GetBasket;
using gamestore.api.Features.Baskets.UpsertBasket;

namespace gamestore.api.Features.Baskets;

public static class BasketsEndpoints
{
    public static void MapBaskets(this WebApplication app)
    {
        RouteGroupBuilder? group = app.MapGroup("/baskets");
        group.MapUpsertBasket();
        group.MapGetBasket();
    }
}
