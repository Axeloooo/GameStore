using FluentAssertions;
using GameStore.Api.Features.Baskets;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.UnitTests.TestSupport;
using GameStore.Data;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Npgsql;

namespace GameStore.Api.UnitTests.Features.Orders;

public class OrderCreatorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 12, 30, 0, TimeSpan.Zero);

    private readonly InMemoryGameStoreContext database = new();
    private readonly TimeProvider timeProviderStub = Substitute.For<TimeProvider>();
    private readonly ILogger<OrderCreator> loggerStub = Substitute.For<ILogger<OrderCreator>>();

    public OrderCreatorTests()
    {
        // Stub: a canned answer, so dates in the created order are deterministic.
        timeProviderStub.GetUtcNow().Returns(Now);
    }

    // BasketItemsProvider is a concrete class with a non-virtual method, so it
    // cannot be replaced by an NSubstitute stub. The real provider runs over the
    // same in-memory context, exactly like the scoped DI registration does.
    private OrderCreator CreateSut(GameStoreContext context)
        => new(context, new BasketItemsProvider(context), timeProviderStub, loggerStub);

    [Fact]
    public async Task GetOrCreateOrderAsync_BasketWithItems_CreatesPendingOrderFromBasket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var halo = InMemoryGameStoreContext.NewGame("Halo", 59.99m);
        var fifa = InMemoryGameStoreContext.NewGame("FIFA", 69.99m);
        await database.SeedBasketAsync(userId, (halo, 1), (fifa, 2));

        using var context = database.Create();
        var sut = CreateSut(context);

        var expected = new Order
        {
            CustomerId = userId,
            OperationId = operationId,
            Status = OrderStatus.Pending,
            Created = Now,
            LastUpdated = Now,
            Items =
            [
                new OrderItem
                {
                    ProductId = halo.Id,
                    ProductName = "Halo",
                    Price = 59.99m,
                    Quantity = 1,
                    ImageUri = halo.ImageUri
                },
                new OrderItem
                {
                    ProductId = fifa.Id,
                    ProductName = "FIFA",
                    Price = 69.99m,
                    Quantity = 2,
                    ImageUri = fifa.ImageUri
                }
            ]
        };

        // Act
        var result = await sut.GetOrCreateOrderAsync(userId, operationId);

        // Assert
        result.EmptyBasket.Should().BeFalse();
        result.Order.Should().BeEquivalentTo(expected, options => options
            .Excluding(order => order.Id)
            .For(order => order.Items).Exclude(item => item.Id));
        result.Order!.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public async Task GetOrCreateOrderAsync_BasketOfGivenSize_CreatesOneOrderItemPerBasketItem(int basketSize)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var basketItems = Enumerable.Range(1, basketSize)
            .Select(i => (Game: InMemoryGameStoreContext.NewGame($"Game {i}", 10m + i), Quantity: i))
            .ToArray();
        await database.SeedBasketAsync(userId, basketItems);

        using var context = database.Create();
        var sut = CreateSut(context);

        // Act
        var result = await sut.GetOrCreateOrderAsync(userId, Guid.NewGuid());

        // Assert
        var items = result.Order!.Items;
        items.Should().HaveCount(basketSize);
        items.Should().OnlyHaveUniqueItems(item => item.ProductId);
        items.Select(item => item.ProductId)
             .Should().BeEquivalentTo(basketItems.Select(item => item.Game.Id));
        items.Should().AllSatisfy(item => item.Quantity.Should().BePositive());
        items.Should().ContainSingle(item => item.ProductName == $"Game {basketSize}"
                                             && item.Quantity == basketSize);

        using var assertContext = database.Create();
        var savedOrder = await assertContext.Orders.Include(order => order.Items).SingleAsync();
        savedOrder.Items.Should().HaveCount(basketSize);
    }

    [Fact]
    public async Task GetOrCreateOrderAsync_NoBasket_ReturnsEmptyBasketAndSavesNothing()
    {
        // Arrange
        using var context = database.Create();
        var sut = CreateSut(context);

        // Act
        var result = await sut.GetOrCreateOrderAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.EmptyBasket.Should().BeTrue();
        result.Order.Should().BeNull();

        using var assertContext = database.Create();
        (await assertContext.Orders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetOrCreateOrderAsync_NewOrder_RaisesSavedChangesForOrderAndItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await database.SeedBasketAsync(
            userId,
            (InMemoryGameStoreContext.NewGame("Halo", 59.99m), 1),
            (InMemoryGameStoreContext.NewGame("FIFA", 69.99m), 1));

        using var context = database.Create();
        var sut = CreateSut(context);
        using var monitoredContext = context.Monitor();

        // Act
        await sut.GetOrCreateOrderAsync(userId, Guid.NewGuid());

        // Assert: DbContext.SavedChanges is a real .NET event on GameStoreContext.
        monitoredContext.Should().Raise(nameof(DbContext.SavingChanges));
        monitoredContext.Should()
            .Raise(nameof(DbContext.SavedChanges))
            .WithArgs<SavedChangesEventArgs>(args => args.EntitiesSavedCount == 3); // 1 order + 2 items
    }

    [Fact]
    public async Task GetOrCreateOrderAsync_SameOperationIdTwice_ReturnsExistingOrderWithoutSaving()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await database.SeedBasketAsync(userId, (InMemoryGameStoreContext.NewGame("Halo", 59.99m), 1));

        Order firstOrder;
        using (var firstContext = database.Create())
        {
            firstOrder = (await CreateSut(firstContext).GetOrCreateOrderAsync(userId, operationId)).Order!;
        }

        using var context = database.Create();
        var sut = CreateSut(context);
        using var monitoredContext = context.Monitor();

        // Act
        var result = await sut.GetOrCreateOrderAsync(userId, operationId);

        // Assert
        result.Order!.Id.Should().Be(firstOrder.Id);
        result.Order.Items.Should().ContainSingle();
        monitoredContext.Should().NotRaise(nameof(DbContext.SavingChanges));

        using var assertContext = database.Create();
        (await assertContext.Orders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateOrderAsync_UniqueViolationAndCompetingOrderExists_ReturnsCompetingOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var halo = InMemoryGameStoreContext.NewGame("Halo", 59.99m);
        await database.SeedBasketAsync(userId, (halo, 1));

        var competingOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = userId,
            OperationId = operationId,
            Status = OrderStatus.Pending,
            Created = Now.AddSeconds(-1),
            LastUpdated = Now.AddSeconds(-1),
            Items = [new OrderItem { ProductId = halo.Id, ProductName = halo.Name, Price = halo.Price, Quantity = 1, ImageUri = halo.ImageUri }]
        };

        // Simulates a concurrent request that inserted the same OperationId
        // between our "existing order" check and our SaveChanges.
        var interceptor = new UniqueViolationInterceptor(async () =>
        {
            using var competingContext = database.Create();
            competingContext.Orders.Add(competingOrder);
            await competingContext.SaveChangesAsync();
        });

        using var context = database.Create(interceptor);
        var sut = CreateSut(context);

        // Act
        var result = await sut.GetOrCreateOrderAsync(userId, operationId);

        // Assert
        result.Order!.Id.Should().Be(competingOrder.Id);
        result.Order.Created.Should().Be(Now.AddSeconds(-1));

        using var assertContext = database.Create();
        (await assertContext.Orders.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateOrderAsync_UniqueViolationAndNoCompetingOrder_RethrowsDbUpdateException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await database.SeedBasketAsync(userId, (InMemoryGameStoreContext.NewGame("Halo", 59.99m), 1));

        using var context = database.Create(new UniqueViolationInterceptor());
        var sut = CreateSut(context);

        // Act
        var act = () => sut.GetOrCreateOrderAsync(userId, Guid.NewGuid());

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
}
