using System.Net.Http.Json;
using AutoFixture;
using GameStore.Api.Features.Baskets.GetBasket;
using GameStore.Api.Features.Baskets.UpsertBasket;
using GameStore.Data.Models;
using Shouldly;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using GameStore.IntegrationTests.Authentication;

namespace GameStore.IntegrationTests.Api;

public class BasketEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder().Build();
    private readonly Fixture fixture = new();
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public async Task GetById_WithExistingBasket_ReturnsBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var db = application.CreateDbContext();

        var game = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();

        var basket = new CustomerBasket
        {
            Id = userId,
            Items =
            [
                new() { GameId = game.Id, Quantity = 1 }
            ]
        };

        db.Games.Add(game);
        db.Baskets.Add(basket);

        await db.SaveChangesAsync(CancellationToken);

        var client = application.CreateClient();

        // Act
        var response = await client.GetAsync($"/baskets/{userId}", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var basketResponse = await response.Content.ReadFromJsonAsync<BasketDto>(CancellationToken);

        basketResponse?.CustomerId.ShouldBe(basket.Id);
        basketResponse?.Items.Count().ShouldBe(1);
        var item = basketResponse?.Items.First();
        item.ShouldNotBeNull();
        item.Id.ShouldBe(game.Id);
        item.Quantity.ShouldBe(basket.Items.First().Quantity);
        item.Name.ShouldBe(game.Name);
        item.Price.ShouldBe(game.Price);
    }

    [Fact]
    public async Task UpsertBasket_CreatesBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var db = application.CreateDbContext();
        var game1 = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();
        var game2 = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();
        db.Games.AddRange(game1, game2);
        await db.SaveChangesAsync(CancellationToken);

        var client = application.CreateClient();

        // Act: Create basket
        var upsertDto = new UpsertBasketDto(
        [
            new UpsertBasketItemDto(game1.Id, 2),
            new UpsertBasketItemDto(game2.Id, 1)
        ]);
        var putResponse = await client.PutAsJsonAsync($"/baskets/{userId}", upsertDto, CancellationToken);

        // Assert: Should return NoContent
        putResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        // Verify basket in DB
        var basketInDb = await db.Baskets.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == userId, CancellationToken);
        basketInDb.ShouldNotBeNull();
        basketInDb!.Items.Count.ShouldBe(2);
        basketInDb.Items.Any(i => i.GameId == game1.Id && i.Quantity == 2).ShouldBeTrue();
        basketInDb.Items.Any(i => i.GameId == game2.Id && i.Quantity == 1).ShouldBeTrue();
    }

    [Fact]
    public async Task GetById_WithDifferentUserId_ReturnsForbidden()
    {
        // Arrange
        var basketOwnerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: differentUserId.ToString(),  // Authenticated as different user
            scope: Scopes.ApiAccessScope);

        var db = application.CreateDbContext();

        var game = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .Create();

        var basket = new CustomerBasket
        {
            Id = basketOwnerId,  // Basket belongs to different user
            Items =
            [
                new() { GameId = game.Id, Quantity = 1 }
            ]
        };

        db.Games.Add(game);
        db.Baskets.Add(basket);

        await db.SaveChangesAsync(CancellationToken);

        var client = application.CreateClient();

        // Act: Try to access another user's basket
        var response = await client.GetAsync($"/baskets/{basketOwnerId}", CancellationToken);

        // Assert: Should be forbidden
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
    }

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
