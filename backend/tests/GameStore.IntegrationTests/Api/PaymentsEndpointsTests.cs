using System.Net.Http.Json;
using AutoFixture;
using GameStore.Api.Features.Payments.Constants;
using GameStore.Api.Features.Payments.CreateCheckoutSession;
using GameStore.Api.Shared.Stripe;
using GameStore.Data.Models;
using GameStore.IntegrationTests.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Stripe;
using Stripe.Checkout;
using Testcontainers.PostgreSql;

namespace GameStore.IntegrationTests.Api;

public class PaymentsEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder().Build();
    private readonly Fixture fixture = new();
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));
    }

    #region Create Checkout Session

    [Fact]
    public async Task CreateCheckoutSession_WithValidBasket_CreatesSessionAndPersistsPendingOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Add game and basket directly to the DB
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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var operationId = Guid.NewGuid();
        var request = new CreateCheckoutSessionDto(operationId);

        // Act
        var response = await client.PostAsJsonAsync("/payments/checkout",
                                                    request,
                                                    CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299

        var responseDto = await response.Content.ReadFromJsonAsync<CheckoutSessionDto>(CancellationToken);
        responseDto!.ClientSecret.ShouldNotBeNullOrEmpty();
        responseDto!.OrderId.ShouldNotBe(Guid.Empty);

        // Assert (DB): order exists with correct snapshot + state
        await using var verifyDb = application.CreateDbContext();
        var order = await verifyDb.Orders.FindAsync([responseDto.OrderId], CancellationToken);

        order.ShouldNotBeNull();
        order!.CustomerId.ShouldBe(userId);
        order.OperationId.ShouldBe(operationId);
        order.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithSameOperationId_ReturnsSameOrderId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Seed game and basket
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

        var operationId = Guid.NewGuid();
        var request = new CreateCheckoutSessionDto(operationId);

        // Act
        var response1 = await client.PostAsJsonAsync("/payments/checkout", request, CancellationToken);
        var dto1 = await response1.Content.ReadFromJsonAsync<CheckoutSessionDto>(CancellationToken);

        var response2 = await client.PostAsJsonAsync("/payments/checkout", request, CancellationToken);
        var dto2 = await response2.Content.ReadFromJsonAsync<CheckoutSessionDto>(CancellationToken);

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        dto1.ShouldNotBeNull();
        dto2.ShouldNotBeNull();
        dto1!.OrderId.ShouldNotBe(Guid.Empty);
        dto2!.OrderId.ShouldBe(dto1.OrderId);

        // Assert (DB): only one order exists for this operation id
        await using var verifyDb = application.CreateDbContext();
        var orders = await verifyDb.Orders
            .Where(o => o.OperationId == operationId)
            .ToListAsync(CancellationToken);
        orders.Count.ShouldBe(1);
        orders[0].Id.ShouldBe(dto1.OrderId);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEmptyBasket_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Keep the shopping basket empty

        var operationId = Guid.NewGuid();
        var request = new CreateCheckoutSessionDto(operationId);

        // Act
        var response = await client.PostAsJsonAsync("/payments/checkout", request, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithValidBasket_SetsCorrectLineItemOnStripeCheckoutSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var application = new GameStoreWebApplicationFactory(
            postgres,
            userId: userId.ToString(),
            scope: Scopes.ApiAccessScope);

        var client = application.CreateClient();

        // Seed one game with a deterministic price and a basket with quantity 1
        var db = application.CreateDbContext();
        // Simple positive price less than $100 (no decimal requirements)
        var price = (decimal)Random.Shared.Next(1, 100); // 1..99
        var game = fixture.Build<Game>()
            .With(g => g.Genre, db.Genres.First())
            .With(g => g.Price, price)
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

        var operationId = Guid.NewGuid();
        var request = new CreateCheckoutSessionDto(operationId);

        // Act
        var response = await client.PostAsJsonAsync("/payments/checkout", request, CancellationToken);
        response.EnsureSuccessStatusCode();

        // Assert: Stripe Checkout Session was created with the correct line item
        var expectedCents = (long)(price * 100m);
        var sessionService = application.Services.GetRequiredService<SessionService>();
        await sessionService.Received(1).CreateAsync(
            Arg.Is<SessionCreateOptions>(o =>
                o.LineItems.Count == 1 &&
                o.LineItems[0].PriceData.UnitAmount == expectedCents &&
                o.LineItems[0].Quantity == 1),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Stripe Webhook

    [Fact]
    public async Task StripeWebhook_WithCheckoutSessionCompleted_UpdatesOrderStatusAndCreatesOutboxMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stripeEventFactoryStub = Substitute.For<IStripeEventFactory>();
        var app = new GameStoreWebApplicationFactory(postgres,
            userId: userId.ToString(),
            stripeEventFactoryOverride: stripeEventFactoryStub);

        var db = app.CreateDbContext();

        // Seed a pending order that matches what the webhook will reference
        var order = fixture.Build<Order>()
            .With(o => o.Id, Guid.NewGuid())
            .With(o => o.CustomerId, userId)
            .With(o => o.Status, OrderStatus.Pending)
            .With(o => o.PaymentId, (string?)null)
            .With(o => o.PaymentCardBrand, (string?)null)
            .With(o => o.PaymentCardLast4, (string?)null)
            .Create();

        db.Orders.Add(order);
        await db.SaveChangesAsync(CancellationToken);

        // Build Session with required metadata
        var paymentIntentId = Guid.NewGuid().ToString();
        var session = new Session
        {
            Id = Guid.NewGuid().ToString(),
            Object = "checkout.session",
            PaymentIntentId = paymentIntentId,
            AmountTotal = 5000, // $50.00 in cents
            Metadata = new Dictionary<string, string>
            {
                [MetadataKeys.OrderId] = order.Id.ToString()
            }
        };

        // Stub the event factory so the endpoint doesn't need real HMAC verification or JSON shape
        var evt = new Event
        {
            Id = Guid.NewGuid().ToString(),
            Type = EventTypes.CheckoutSessionCompleted,
            Data = new EventData { Object = session }
        };

        stripeEventFactoryStub.Create(Arg.Any<string>(), Arg.Any<string>()).Returns(evt);

        // Configure PaymentIntentService stub to return the correct payment intent ID
        var paymentMethod = new PaymentMethod
        {
            Id = "pm_test",
            Card = new PaymentMethodCard { Brand = "visa", Last4 = "4242" }
        };
        var paymentIntent = new PaymentIntent
        {
            Id = paymentIntentId,
            PaymentMethod = paymentMethod
        };
        var paymentIntentService = app.Services.GetRequiredService<PaymentIntentService>();
        paymentIntentService.GetAsync(
            paymentIntentId,
            Arg.Any<PaymentIntentGetOptions>(),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paymentIntent));

        var client = app.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/payments/stripe-webhook",
                                                    evt,
                                                    CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var verifyDb = app.CreateDbContext();
        var updated = await verifyDb.Orders.AsNoTracking().SingleAsync(o => o.Id == order.Id, CancellationToken);
        updated.Status.ShouldBe(OrderStatus.Processing);
        updated.PaymentId.ShouldBe(paymentIntentId);
        updated.PaymentCardBrand.ShouldBe("visa");
        updated.PaymentCardLast4.ShouldBe("4242");

        // Verify that an outbox message was created
        var outboxMessage = await verifyDb.OutboxMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.CorrelationId == order.Id.ToString(), CancellationToken);

        outboxMessage.ShouldNotBeNull();
        outboxMessage!.MessageType.ShouldBe("OrderPaid");
        outboxMessage.QueueName.ShouldBe("orders");
        outboxMessage.MessageId.ShouldBe(paymentIntentId);
        outboxMessage.ProcessedAt.ShouldBeNull();
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
