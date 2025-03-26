using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using gamestore.api.Data;
using gamestore.api.Features.Games.Constants;
using gamestore.api.Models;
using gamestore.api.Shared.Authorization;
using gamestore.api.Shared.FileUpload;
using Microsoft.AspNetCore.Mvc;

namespace gamestore.api.Features.Games.CreateGame;

public static class CreateGameEndpoint
{
    private const string DefaultImageUri = "https://placehold.co/100";

    public static void MapCreateGame(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "/",
                async (
                    [FromForm] CreateGameDto gameDto,
                    GameStoreContext dbContext,
                    FileUploader fileUploader,
                    ClaimsPrincipal user
                ) =>
                {
                    if (user?.Identity?.IsAuthenticated == false)
                    {
                        return Results.Unauthorized();
                    }

                    string? currentUserId =
                        user?.FindFirstValue(JwtRegisteredClaimNames.Email)
                        ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        return Results.Unauthorized();
                    }

                    string imageUri = DefaultImageUri;

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

                        imageUri = fileUploadResult.FileUrl!;
                    }

                    Game game = new()
                    {
                        Name = gameDto.Name,
                        GenreId = gameDto.GenreId,
                        Price = gameDto.Price,
                        Description = gameDto.Description,
                        ReleaseDate = gameDto.ReleaseDate,
                        ImageUri = imageUri,
                        LastUpdatedBy = currentUserId,
                    };

                    dbContext.Games.Add(game);

                    await dbContext.SaveChangesAsync();

                    return Results.CreatedAtRoute(
                        EndpointNames.GetGame,
                        new { id = game.Id },
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
            .WithParameterValidation()
            .DisableAntiforgery()
            .RequireAuthorization(Policies.AdminAccess);
    }
}
