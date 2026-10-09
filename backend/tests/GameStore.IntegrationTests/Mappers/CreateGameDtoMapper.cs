using System.Net.Http.Headers;
using GameStore.Api.Features.Games.CreateGame;

namespace GameStore.IntegrationTests.Mappers;

public static class CreateGameDtoMapper
{
    public static MultipartFormDataContent ToMultiPartFormDataContent(
        this CreateGameDto dto, bool includeImage = false)
    {
        var formData = new MultipartFormDataContent
        {
            { new StringContent(dto.Name), nameof(dto.Name) },
            { new StringContent(dto.GenreId.ToString()), nameof(dto.GenreId) },
            { new StringContent(dto.Price.ToString()), nameof(dto.Price) },
            { new StringContent(dto.ReleaseDate.ToString()), nameof(dto.ReleaseDate) },
            { new StringContent(dto.Description), nameof(dto.Description) }
        };

        if (includeImage)
        {
            var imageFileContent = new ByteArrayContent([1, 2, 3]);
            imageFileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            formData.Add(imageFileContent, "ImageFile", $"{Guid.NewGuid()}.png");
        }

        return formData;
    }
}


