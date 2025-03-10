using gamestore.api.Features.Games.CreateGame;
using gamestore.api.Features.Games.DeleteGame;
using gamestore.api.Features.Games.GetGame;
using gamestore.api.Features.Games.GetGames;
using gamestore.api.Features.Games.UpdateGame;

namespace gamestore.api.Features.Games;

public static class GamesEndpoints
{
    public static void MapGames(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder? group = app.MapGroup("/games");
        group.MapCreateGame();
        group.MapGetGame();
        group.MapGetGames();
        group.MapUpdateGame();
        group.MapDeleteGame();
    }
}
