using GameStore.Data;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameStore.Api.UnitTests.TestSupport;

// Builds GameStoreContext instances backed by the EF Core InMemory provider.
// Every test gets its own database name, so tests never share state. Contexts
// created with the same database name see the same data, which lets a test
// seed with one context, run the code under test with another and assert with
// a third (no change tracker leaks between Arrange, Act and Assert).
internal sealed class InMemoryGameStoreContext
{
    private readonly string databaseName = $"gamestore-unit-{Guid.NewGuid()}";

    // An explicit root guarantees that every context created here (with or
    // without interceptors) reads and writes the same in-memory store.
    private readonly InMemoryDatabaseRoot databaseRoot = new();

    public GameStoreContext Create(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<GameStoreContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .AddInterceptors(interceptors)
            .Options;

        return new GameStoreContext(options);
    }

    public static Game NewGame(string name, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        GenreId = Guid.NewGuid(),
        Price = price,
        ReleaseDate = new DateOnly(2024, 1, 1),
        Description = $"{name} description",
        ImageUri = $"https://images.example.test/{name.Replace(' ', '-').ToLowerInvariant()}.png",
        LastUpdatedBy = "unit-tests"
    };

    public async Task<CustomerBasket> SeedBasketAsync(Guid userId, params (Game Game, int Quantity)[] items)
    {
        using var context = Create();

        var basket = new CustomerBasket
        {
            Id = userId,
            Items = [.. items.Select(item => new BasketItem
            {
                GameId = item.Game.Id,
                Game = item.Game,
                Quantity = item.Quantity
            })]
        };

        context.Baskets.Add(basket);
        await context.SaveChangesAsync();

        return basket;
    }
}
