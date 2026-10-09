using GameStore.Data;
using GameStore.Api.Features.Games.Constants;
using GameStore.Data.Models;
using GameStore.Api.Shared.Cdn;

namespace GameStore.Api.Features.Games.GetGame;

public static class GetGameEndpoint
{
    public static void MapGetGame(this IEndpointRouteBuilder app)
    {
        // GET /games/122233-434d-43434....
        app.MapGet("/{id}", async (
            Guid id,
            GameStoreContext dbContext,
            CdnUrlTransformer cdnUrlTransformer,
            ILogger<Program> logger) =>
        {
            Game? game = await dbContext.Games.FindAsync(id);

            return game is null ? Results.NotFound() : Results.Ok(
                                new GameDetailsDto(
                                    game.Id,
                                    game.Name,
                                    game.GenreId,
                                    game.Price,
                                    game.ReleaseDate,
                                    game.Description,
                                    cdnUrlTransformer.TransformToCdnUrl(game.ImageUri),
                                    game.LastUpdatedBy
                                ));
        })
        .WithName(EndpointNames.GetGame)
        .AllowAnonymous();
    }
}
