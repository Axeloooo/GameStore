using FluentAssertions;
using GameStore.Api.Features.Baskets.GetBasket;

namespace GameStore.Api.UnitTests.Features.Baskets;

// GameStore.Api has no standalone DTO mapper classes: entity-to-DTO mapping is
// written inline in the endpoint lambdas (covered by the integration tests).
// BasketDto is the one DTO with logic of its own, its computed TotalAmount.
public class BasketDtoTests
{
    private static readonly Guid CustomerId = Guid.Parse("9c3e1b7a-2d4f-4a6b-8c1d-0e2f3a4b5c6d");

    // A fixed id keeps theory display names identical from run to run; the id
    // plays no part in the total.
    private static readonly Guid GameId = Guid.Parse("1a2b3c4d-0000-4000-8000-000000000001");

    private static BasketItemDto Item(string name, decimal price, int quantity)
        => new(GameId, name, price, quantity, $"https://images.example.test/{name}.png");

    // MemberData: decimal values cannot be passed through [InlineData].
    public static TheoryData<BasketItemDto[], decimal> BasketsAndTotals => new()
    {
        { [], 0m },
        { [Item("halo", 59.99m, 1)], 59.99m },
        { [Item("halo", 59.99m, 2), Item("fifa", 9.99m, 3)], 149.95m },
        { [Item("indie", 0.10m, 3)], 0.30m }, // decimal keeps cents exact
        { [Item("free", 0m, 5), Item("halo", 59.99m, 1)], 59.99m }
    };

    [Theory]
    [MemberData(nameof(BasketsAndTotals))]
    public void TotalAmount_GivenItems_IsSumOfPriceTimesQuantity(BasketItemDto[] items, decimal expectedTotal)
    {
        // Arrange
        var sut = new BasketDto(CustomerId, items);

        // Act
        var total = sut.TotalAmount;

        // Assert
        total.Should().Be(expectedTotal);
        total.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void TotalAmount_ItemsChangeAfterConstruction_IsRecomputedOnRead()
    {
        // Arrange
        var items = new List<BasketItemDto> { Item("halo", 59.99m, 1) };
        var sut = new BasketDto(CustomerId, items);
        var before = sut.TotalAmount;

        // Act
        items.Add(Item("fifa", 10m, 2));

        // Assert: TotalAmount is a computed property, not a value captured at construction.
        before.Should().Be(59.99m);
        sut.TotalAmount.Should().Be(79.99m);
        sut.TotalAmount.Should().BeGreaterThan(before);
    }
}
