using FluentAssertions;
using GameStore.Api.Features.Baskets;
using GameStore.Api.UnitTests.TestSupport;
using GameStore.Data.Models;

namespace GameStore.Api.UnitTests.Features.Baskets;

// GameStoreContext runs on the EF Core InMemory provider here: the model maps a
// PostgreSQL sequence (HasSequence + nextval() default) that only Npgsql
// understands, and the InMemory provider ignores relational-only configuration.
// Each test seeds through one context and queries through a fresh one, so the
// Include/ThenInclude calls are really exercised (no change-tracker fix-up).
public class BasketItemsProviderTests
{
    private readonly InMemoryGameStoreContext database = new();

    [Fact]
    public async Task GetBasketItemsAsync_NoBasketForUser_ReturnsEmptyList()
    {
        // Arrange
        using var context = database.Create();
        var sut = new BasketItemsProvider(context);

        // Act
        var items = await sut.GetBasketItemsAsync(Guid.NewGuid());

        // Assert
        items.Should().NotBeNull();
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBasketItemsAsync_EmptyBasket_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await database.SeedBasketAsync(userId);

        using var context = database.Create();
        var sut = new BasketItemsProvider(context);

        // Act
        var items = await sut.GetBasketItemsAsync(userId);

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBasketItemsAsync_BasketWithItems_ReturnsItemsWithGameLoaded()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var halo = InMemoryGameStoreContext.NewGame("Halo", 59.99m);
        var fifa = InMemoryGameStoreContext.NewGame("FIFA", 69.99m);
        await database.SeedBasketAsync(userId, (halo, 1), (fifa, 3));

        using var context = database.Create();
        var sut = new BasketItemsProvider(context);

        // Act
        var items = await sut.GetBasketItemsAsync(userId);

        // Assert
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(item =>
        {
            item.CustomerBasketId.Should().Be(userId);
            item.Game.Should().NotBeNull();
        });
        items.Should().ContainSingle(item => item.GameId == fifa.Id)
             .Which.Should().BeEquivalentTo(new { Quantity = 3, Game = new { Name = "FIFA", Price = 69.99m } });
    }

    [Fact]
    public async Task GetBasketItemsAsync_SeveralUsersHaveBaskets_ReturnsOnlyRequestedUsersItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var halo = InMemoryGameStoreContext.NewGame("Halo", 59.99m);
        await database.SeedBasketAsync(userId, (halo, 1));
        await database.SeedBasketAsync(otherUserId, (InMemoryGameStoreContext.NewGame("FIFA", 69.99m), 2));

        using var context = database.Create();
        var sut = new BasketItemsProvider(context);

        // Act
        var items = await sut.GetBasketItemsAsync(userId);

        // Assert
        items.Should().ContainSingle()
             .Which.GameId.Should().Be(halo.Id);
        items.Should().NotContain(item => item.CustomerBasketId == otherUserId);
    }

    [Fact]
    public async Task GetBasketItemsAsync_BasketWithItems_ReturnsReadOnlyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await database.SeedBasketAsync(userId, (InMemoryGameStoreContext.NewGame("Halo", 59.99m), 1));

        using var context = database.Create();
        var sut = new BasketItemsProvider(context);

        // Act
        var items = await sut.GetBasketItemsAsync(userId);

        // Assert: callers get a read-only view, they cannot mutate the basket through it.
        var asList = items.Should().BeAssignableTo<IList<BasketItem>>().Subject;
        asList.IsReadOnly.Should().BeTrue();
    }
}
