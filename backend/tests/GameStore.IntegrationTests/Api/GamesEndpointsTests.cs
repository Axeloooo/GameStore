using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoFixture;
using GameStore.Api.Features.Games.CreateGame;
using GameStore.Api.Features.Games.GetGames;
using GameStore.Api.Features.Games.UpdateGame;
using GameStore.Api.Shared.Authorization;
using GameStore.Data.Models;
using GameStore.IntegrationTests.Authentication;
using GameStore.IntegrationTests.Mappers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;

namespace GameStore.IntegrationTests.Api;

public class GamesEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder().Build();
    private readonly AzuriteContainer azurite = new AzuriteBuilder()
                                                .WithImage("mcr.microsoft.com/azure-storage/azurite:3.35.0")
                                                .Build();
    private readonly Fixture fixture = new();
    private static CancellationToken CancellationToken
        => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync(CancellationToken);
        await azurite.StartAsync(CancellationToken);
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    #region GetAll

    [Fact]
    public async Task GetAll_WithValidRequest_ReturnsGames()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            azuriteContainer: azurite);

        var db = application.CreateDbContext();

        var game = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();

        db.Games.Add(game);

        await db.SaveChangesAsync(CancellationToken);

        var client = application.CreateClient();

        // Act
        var response = await client.GetAsync("/games", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var expectedDto = new GameSummaryDto(
                        game.Id,
                        game.Name,
                        game.Genre!.Name,
                        game.Price,
                        game.ReleaseDate,
                        game.ImageUri,
                        game.LastUpdatedBy);
        var gamesResponse = await response.Content.ReadFromJsonAsync<GamesPageDto>(CancellationToken);
        gamesResponse.ShouldNotBeNull();
        var singleGame = gamesResponse!.Data.ShouldHaveSingleItem();

        singleGame.ShouldNotBeNull();
        singleGame.ShouldBeEquivalentTo(expectedDto);
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetById_WithValidId_ReturnsGame()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            azurite);

        var db = application.CreateDbContext();

        var game = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();

        db.Games.Add(game);

        await db.SaveChangesAsync(CancellationToken);

        var client = application.CreateClient();

        // Act
        var response = await client.GetAsync($"/games/{game.Id}", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var expected = new GameDetailsDto(
            game.Id,
            game.Name,
            game.GenreId,
            game.Price,
            game.ReleaseDate,
            game.Description,
            game.ImageUri,
            game.LastUpdatedBy);

        var actual = await response.Content.ReadFromJsonAsync<GameDetailsDto>(CancellationToken);
        actual.ShouldNotBeNull();
        actual.ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetById_WithUnexistingId_ReturnsNotFound()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            azurite);

        var client = application.CreateClient();

        // Act
        var response = await client.GetAsync($"/games/{Guid.NewGuid()}", CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Post

    [Fact]
    public async Task Post_WithValidRequest_CreatesGame()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            azurite,
            userId: userId,
            role: Roles.Admin,
            scope: Scopes.ApiAccessScope);

        var db = application.CreateDbContext();

        var client = application.CreateClient();

        var createGameDto = new CreateGameDto(
            Name: "Test Game",
            GenreId: db.Genres.First().Id,
            Price: 59.99m,
            ReleaseDate: new DateOnly(2024, 12, 25),
            Description: "A test game description"
        );

        var request = createGameDto.ToMultiPartFormDataContent();

        // Act
        var response = await client.PostAsync("/games", request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var gameResponse = await response.Content.ReadFromJsonAsync<GameDetailsDto>(CancellationToken);
        gameResponse.ShouldNotBeNull();

        // Verify the game was persisted correctly in the database
        var createdGame = await db.Games.FindAsync([gameResponse.Id], CancellationToken);
        createdGame.ShouldNotBeNull();
        createdGame.Name.ShouldBe(createGameDto.Name);
        createdGame.GenreId.ShouldBe(createGameDto.GenreId);
        createdGame.Price.ShouldBe(createGameDto.Price);
        createdGame.ReleaseDate.ShouldBe(createGameDto.ReleaseDate);
        createdGame.Description.ShouldBe(createGameDto.Description);
        createdGame.ImageUri.ShouldNotBeNullOrEmpty();
        createdGame.LastUpdatedBy.ShouldBe(userId);
    }

    [Fact]
    public async Task Post_MissingRequiredInfo_ReturnsBadRequest()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            azuriteContainer: azurite,
            role: Roles.Admin,
            scope: Scopes.ApiAccessScope);

        var db = application.CreateDbContext();

        var client = application.CreateClient();

        var createGameDto = new CreateGameDto(
            Name: string.Empty,
            GenreId: db.Genres.First().Id,
            Price: 59.99m,
            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Description: "A test game description"
        );

        var request = createGameDto.ToMultiPartFormDataContent();

        // Act
        var response = await client.PostAsync("/games", request, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        object? errorsJson = problemDetails?.Extensions["errors"];
        errorsJson.ShouldNotBeNull();

        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson!.ToString()!);
        errors.ShouldNotBeNull();
        errors!.ContainsKey(nameof(createGameDto.Name)).ShouldBeTrue();
    }

    [Fact]
    public async Task Post_WithImageFile_CreatesGameWithImageUri()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            role: Roles.Admin,
            scope: Scopes.ApiAccessScope,
            azuriteContainer: azurite);

        var db = application.CreateDbContext();

        var client = application.CreateClient();

        var createGameDto = new CreateGameDto(
            Name: "Test Game",
            GenreId: db.Genres.First().Id,
            Price: 59.99m,
            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Description: "A test game description"
        );

        MultipartFormDataContent request = createGameDto.ToMultiPartFormDataContent(includeImage: true);

        // Act
        var response = await client.PostAsync("/games", request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var gameResponse = await response.Content.ReadFromJsonAsync<GameDetailsDto>(CancellationToken);
        gameResponse.ShouldNotBeNull();
        gameResponse!.Id.ShouldNotBe(Guid.Empty);
        var createdGame = await db.Games.FindAsync([gameResponse?.Id], CancellationToken);
        createdGame.ShouldNotBeNull();

        createdGame!.ImageUri.ShouldNotBeNullOrWhiteSpace();

        // Retrieve the image via HTTP and validate status, content-type, and body
        using var http = new HttpClient();
        var imgResponse = await http.GetAsync(createdGame.ImageUri, CancellationToken);
        imgResponse.EnsureSuccessStatusCode();
        imgResponse.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
    }

    #endregion

    #region Put

    [Fact]
    public async Task Put_WithValidRequest_UpdatesGame()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            azuriteContainer: azurite,
            userId: userId,
            role: Roles.Admin,
            scope: Scopes.ApiAccessScope);

        var db = application.CreateDbContext();

        var game = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();

        db.Games.Add(game);

        await db.SaveChangesAsync(CancellationToken);

        var client = application.CreateClient();

        var updateGameDto = new UpdateGameDto(
            Name: "Updated Game",
            GenreId: db.Genres.First().Id,
            Price: 39.99m,
            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Description: "Updated description"
        );

        var request = updateGameDto.ToMultiPartFormDataContent();

        // Act
        var response = await client.PutAsync($"/games/{game.Id}", request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        db = application.CreateDbContext();
        var updatedGame = await db.Games.FindAsync(new object?[] { game.Id }, CancellationToken);
        updatedGame.ShouldNotBeNull();
        updatedGame!.Name.ShouldBe(updateGameDto.Name);
        updatedGame.GenreId.ShouldBe(updateGameDto.GenreId);
        updatedGame.Price.ShouldBe(updateGameDto.Price);
        updatedGame.ReleaseDate.ShouldBe(updateGameDto.ReleaseDate);
        updatedGame.Description.ShouldBe(updateGameDto.Description);
    }

    [Fact]
    public async Task Put_NoAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            authenticationSucceeds: false);

        var db = application.CreateDbContext();

        var client = application.CreateClient();

        var updateGameDto = new UpdateGameDto(
            Name: "Updated Game",
            GenreId: db.Genres.First().Id,
            Price: 39.99m,
            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Description: "Updated description"
        );

        var request = updateGameDto.ToMultiPartFormDataContent();

        // Act
        var response = await client.PutAsync($"/games/{Guid.NewGuid()}", request, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_WithValidId_DeletesGame()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            role: Roles.Admin,
            scope: Scopes.ApiAccessScope);

        var db = application.CreateDbContext();

        var game = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();

        db.Games.Add(game);

        await db.SaveChangesAsync(CancellationToken);

        var client = application.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/games/{game.Id}", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        db = application.CreateDbContext();
        var gameInDb = await db.Games.FindAsync(new object?[] { game.Id }, CancellationToken);
        gameInDb.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_UnauthorizedRole_ReturnsForbidden()
    {
        // Arrange
        var application = new GameStoreWebApplicationFactory(
            postgres,
            role: "TestRole",
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/games/{Guid.NewGuid()}", CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        await azurite.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
