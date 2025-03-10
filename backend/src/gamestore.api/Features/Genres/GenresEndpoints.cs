using gamestore.api.Features.Genres.GetGenres;

namespace gamestore.api.Features.Genres;

public static class GenresEndpoints
{
    public static void MapGenres(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder? group = app.MapGroup("/genres");
        group.MapGetGenres();
    }
}
