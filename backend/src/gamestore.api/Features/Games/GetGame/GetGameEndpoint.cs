using gamestore.api.Data;
using gamestore.api.Features.Games.Constants;
using gamestore.api.Models;

namespace gamestore.api.Features.Games.GetGame;

public static class GetGameEndpoint
{
    public static void MapGetGame(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/{id}",
                async (Guid id, GameStoreContext dbContext) =>
                {
                    Game? game = await dbContext.Games.FindAsync(id);
                    return game is null
                        ? Results.NotFound()
                        : Results.Ok(
                            new GameDetailsDto(
                                game.Id,
                                game.Name,
                                game.GenreId,
                                game.Price,
                                game.Description,
                                game.ReleaseDate,
                                game.ImageUri,
                                game.LastUpdatedBy
                            )
                        );
                }
            )
            .WithName(EndpointNames.GetGame)
            .AllowAnonymous();
    }
}
