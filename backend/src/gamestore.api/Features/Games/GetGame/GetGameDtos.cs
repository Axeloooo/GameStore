namespace gamestore.api.Features.Games.GetGame;

public record GameDetailsDto(
    Guid Id,
    string Name,
    Guid GenreId,
    decimal Price,
    string Description,
    DateOnly ReleaseDate,
    string ImageUri,
    string LastUpdatedBy
);
