using System;
using GameStore.Data;
using GameStore.Api.Features.Genres.GetGenres;
using GameStore.Data.Models;

namespace GameStore.Api.Features.Genres;

public static class GenresEndpoints
{
    public static void MapGenres(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/genres");

        group.MapGetGenres();
    }
}
