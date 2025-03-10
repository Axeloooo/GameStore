using System.ComponentModel.DataAnnotations;

namespace gamestore.api.Features.Games.CreateGame;

public record CreateGameDto(
    [Required] [StringLength(50)] string Name,
    Guid GenreId,
    [Range(1, 100)] decimal Price,
    [Required] [StringLength(500)] string Description,
    DateOnly ReleaseDate
)
{
    public IFormFile? ImageFile { get; set; }
}

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
