using System;
using GameStore.Data;
using GameStore.Data.Models;
using GameStore.Api.Shared.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Features.Games.DeleteGame;

public static class DeleteGameEndpoint
{
    public static void MapDeleteGame(this IEndpointRouteBuilder app)
    {
        // DELETE /games/122233-434d-43434....
        app.MapDelete("/{id}", async (Guid id, GameStoreContext dbContext) =>
        {
            await dbContext.Games
                     .Where(game => game.Id == id)
                     .ExecuteDeleteAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(Policies.AdminAccess);
    }
}
