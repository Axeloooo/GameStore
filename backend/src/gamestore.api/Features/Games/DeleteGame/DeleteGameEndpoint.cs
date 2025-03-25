using gamestore.api.Data;
using gamestore.api.Shared.Authorization;
using Microsoft.EntityFrameworkCore;

namespace gamestore.api.Features.Games.DeleteGame;

public static class DeleteGameEndpoint
{
    public static void MapDeleteGame(this IEndpointRouteBuilder app)
    {
        app.MapDelete(
                "/{id}",
                async (Guid id, GameStoreContext dbContext) =>
                {
                    await dbContext.Games.Where(game => game.Id == id).ExecuteDeleteAsync();

                    return Results.NoContent();
                }
            )
            .RequireAuthorization(Policies.AdminAccess);
    }
}
