using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using gamestore.api.Data;
using gamestore.api.Features.Games.Constants;
using gamestore.api.Models;
using gamestore.api.Shared.Authorization;
using gamestore.api.Shared.FileUpload;

namespace gamestore.api.Features.Games.UpdateGame;

public static class UpdateGameEndpoint
{
    public static void MapUpdateGame(this IEndpointRouteBuilder app)
    {
        app.MapPut(
                "/{id}",
                async (
                    Guid id,
                    UpdateGameDto gameDto,
                    GameStoreContext dbContext,
                    FileUploader fileUploader,
                    ClaimsPrincipal user
                ) =>
                {
                    string? currentUserId =
                        user?.FindFirstValue(JwtRegisteredClaimNames.Email)
                        ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        return Results.Unauthorized();
                    }

                    Game? existingGame = await dbContext.Games.FindAsync(id);

                    if (existingGame is null)
                    {
                        return Results.NotFound();
                    }

                    if (gameDto.ImageFile is not null)
                    {
                        var fileUploadResult = await fileUploader.UploadFileAsync(
                            gameDto.ImageFile,
                            StorageNames.GameImagesFolder
                        );

                        if (!fileUploadResult.IsSuccess)
                        {
                            return Results.BadRequest(
                                new { message = fileUploadResult.ErrorMessage }
                            );
                        }

                        existingGame.ImageUri = fileUploadResult.FileUrl!;
                    }

                    existingGame.Name = gameDto.Name;
                    existingGame.GenreId = gameDto.GenreId;
                    existingGame.Price = gameDto.Price;
                    existingGame.ReleaseDate = gameDto.ReleaseDate;

                    await dbContext.SaveChangesAsync();

                    return Results.NoContent();
                }
            )
            .WithParameterValidation()
            .DisableAntiforgery()
            .RequireAuthorization(Policies.AdminAccess);
    }
}
