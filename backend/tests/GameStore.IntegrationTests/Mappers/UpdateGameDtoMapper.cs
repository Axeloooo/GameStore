using System.Net.Http.Headers;
using GameStore.Api.Features.Games.CreateGame;
using GameStore.Api.Features.Games.UpdateGame;

namespace GameStore.IntegrationTests.Mappers;

public static class UpdateGameDtoMapper
{
    public static MultipartFormDataContent ToMultiPartFormDataContent(
        this UpdateGameDto dto)
    {
        var formData = new MultipartFormDataContent
        {
            { new StringContent(dto.Name), nameof(dto.Name) },
            { new StringContent(dto.GenreId.ToString()), nameof(dto.GenreId) },
            { new StringContent(dto.Price.ToString()), nameof(dto.Price) },
            { new StringContent(dto.ReleaseDate.ToString()), nameof(dto.ReleaseDate) },
            { new StringContent(dto.Description), nameof(dto.Description) }
        };

        return formData;
    }
}


