using System.ComponentModel.DataAnnotations;

namespace gamestore.api.Features.Games.UpdateGame;

public record UpdateGameDto(
    [Required] [StringLength(50)] string Name,
    Guid GenreId,
    [Range(1, 100)] decimal Price,
    [Required] [StringLength(500)] string Description,
    DateOnly ReleaseDate
)
{
    public IFormFile? ImageFile { get; set; }
}
