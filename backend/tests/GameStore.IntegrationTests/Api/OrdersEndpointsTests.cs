using System.Net.Http.Json;
using AutoFixture;
using Testcontainers.PostgreSql;
using Shouldly;
using GameStore.Data.Models;
using GameStore.Api.Features.Orders.GetOrder;
using GameStore.IntegrationTests.Authentication;

namespace GameStore.IntegrationTests.Api;


public class OrdersEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder().Build();
    private readonly Fixture fixture = new();

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Get_WithValidOrderId_ReturnsOrderForOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Add an order directly to the DB
        var db = application.CreateDbContext();
        var order = fixture.Build<Order>()
            .With(o => o.CustomerId, userId)
            .With(o => o.Status, OrderStatus.Completed)
            .Without(o => o.Items)
            .Create();

        var orderItem = fixture.Create<OrderItem>();

        order.Items = [orderItem];
        db.Orders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await client.GetAsync($"/orders/{order.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>(TestContext.Current.CancellationToken);
        orderDto.ShouldNotBeNull();
        orderDto.Id.ShouldBe(order.Id);
        orderDto.CustomerId.ShouldBe(userId);
        orderDto.Status.ShouldBe(order.Status.ToString());
        orderDto.TotalAmount.ShouldBe(order.TotalAmount);
        orderDto.Items.ShouldNotBeNull();
        orderDto.Items.Count().ShouldBe(1);
        var dtoItem = orderDto.Items.First();
        dtoItem.ProductId.ShouldBe(orderItem.ProductId);
        dtoItem.ProductName.ShouldBe(orderItem.ProductName);
        dtoItem.Price.ShouldBe(orderItem.Price);
        dtoItem.Quantity.ShouldBe(orderItem.Quantity);
        dtoItem.ImageUri.ShouldBe(orderItem.ImageUri);
        dtoItem.GameCodes.ShouldBe(orderItem.GameCodes);
    }

    [Fact]
    public async Task Get_WithDifferentUserId_ReturnsForbidden()
    {
        // Arrange
        var orderOwnerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: differentUserId.ToString(),  // Authenticated as different user
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Add an order for a different user
        var db = application.CreateDbContext();
        var order = fixture.Build<Order>()
            .With(o => o.CustomerId, orderOwnerId)  // Order belongs to different user
            .With(o => o.Status, OrderStatus.Completed)
            .Without(o => o.Items)
            .Create();

        var orderItem = fixture.Create<OrderItem>();
        order.Items = [orderItem];
        db.Orders.Add(order);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Try to access another user's order
        var response = await client.GetAsync($"/orders/{order.Id}", TestContext.Current.CancellationToken);

        // Assert: Should be forbidden
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WithMultipleOrders_ReturnsPaginatedOrdersForCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Add multiple orders for the current user and another user
        var db = application.CreateDbContext();

        // Create 7 completed orders for current user
        var userOrders = fixture.Build<Order>()
            .With(o => o.CustomerId, userId)
            .With(o => o.Status, OrderStatus.Completed)
            .Without(o => o.Items)
            .CreateMany(7)
            .ToList();

        foreach (var order in userOrders)
        {
            order.Items = [fixture.Create<OrderItem>()];
        }

        // Create 2 orders for another user (should not be returned)
        var otherUserOrders = fixture.Build<Order>()
            .With(o => o.CustomerId, otherUserId)
            .With(o => o.Status, OrderStatus.Completed)
            .Without(o => o.Items)
            .CreateMany(2)
            .ToList();

        foreach (var order in otherUserOrders)
        {
            order.Items = [fixture.Create<OrderItem>()];
        }

        // Add a pending order for current user (should not be returned)
        var pendingOrder = fixture.Build<Order>()
            .With(o => o.CustomerId, userId)
            .With(o => o.Status, OrderStatus.Pending)
            .Without(o => o.Items)
            .Create();
        pendingOrder.Items = [fixture.Create<OrderItem>()];

        db.Orders.AddRange(userOrders);
        db.Orders.AddRange(otherUserOrders);
        db.Orders.Add(pendingOrder);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: Get first page (page size = 5)
        var response = await client.GetAsync("/orders?pageNumber=1&pageSize=5", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var ordersPage = await response.Content.ReadFromJsonAsync<GameStore.Api.Features.Orders.GetOrders.OrdersPageDto>(
            TestContext.Current.CancellationToken);

        ordersPage.ShouldNotBeNull();
        ordersPage.TotalPages.ShouldBe(2); // 7 orders / 5 per page = 2 pages
        ordersPage.Data.Count().ShouldBe(5); // First page has 5 orders

        // All orders should belong to the authenticated user
        ordersPage.Data.ShouldAllBe(o => o.CustomerId == userId);

        // Orders should not include pending orders
        ordersPage.Data.ShouldAllBe(o => o.Status != OrderStatus.Pending.ToString());

        // Orders should be ordered by Created date descending (most recent first)
        var orderedByCreated = ordersPage.Data.OrderByDescending(o => o.Created).ToList();
        ordersPage.Data.SequenceEqual(orderedByCreated, new OrderDtoCreatedComparer()).ShouldBeTrue();
    }

    private class OrderDtoCreatedComparer : IEqualityComparer<GameStore.Api.Features.Orders.GetOrders.OrderDto>
    {
        public bool Equals(GameStore.Api.Features.Orders.GetOrders.OrderDto? x, GameStore.Api.Features.Orders.GetOrders.OrderDto? y)
        {
            if (x is null || y is null) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(GameStore.Api.Features.Orders.GetOrders.OrderDto obj) => obj.Id.GetHashCode();
    }

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
