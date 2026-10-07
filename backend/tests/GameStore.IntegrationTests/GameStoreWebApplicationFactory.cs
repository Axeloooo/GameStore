using AutoFixture;
using Azure.Storage.Blobs;
using DotNet.Testcontainers.Containers;
using GameStore.Api.Shared.Messaging;
using GameStore.Api.Shared.Stripe;
using GameStore.Data;
using GameStore.Data.Models;
using GameStore.IntegrationTests.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Stripe;
using Stripe.Checkout;
using Testcontainers.Azurite;

namespace GameStore.IntegrationTests;

/// <summary>
/// Web application factory for integration tests.
/// Sets up in-memory test server, test database, and stubs for external services.
/// </summary>
internal class GameStoreWebApplicationFactory(
    IDatabaseContainer dbContainer,
    AzuriteContainer? azuriteContainer = null,
    bool authenticationSucceeds = true,
    string? userId = null,
    string? role = null,
    string? scope = null,
    IStripeEventFactory? stripeEventFactoryOverride = null,
    SaveChangesInterceptor? saveChangesInterceptor = null,
    IMessagePublisher? messagePublisher = null)
    : WebApplicationFactory<Program>
{
    public GameStoreContext CreateDbContext()
    {
        var db = Services.GetRequiredService<IDbContextFactory<GameStoreContext>>()
                         .CreateDbContext();
        return db;
    }

    /// <summary>
    /// Configures the test web host by replacing production services with test doubles.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override application configuration with test-specific values
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            // Inject dummy Stripe configuration (non-secret) at runtime for tests only.
            var stripeSettings = new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test",
                ["Stripe:CheckoutReturnUrl"] = "https://example.com/return",
                ["Stripe:EndpointSecret"] = "whsec_test"
            };

            configBuilder.AddInMemoryCollection(stripeSettings);
        });

        // Configure additional services for the host or web application.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<GameStoreContext>));

            var dbContextOptionsBuilder = new DbContextOptionsBuilder<GameStoreContext>();

            dbContextOptionsBuilder.UseNpgsql(dbContainer.GetConnectionString())
                                    .UseAsyncSeeding(async (context, _, cancellationToken) =>
                                    {
                                        SeedGenres(context);
                                        await context.SaveChangesAsync(cancellationToken);
                                    });

            if (saveChangesInterceptor is not null)
            {
                dbContextOptionsBuilder.AddInterceptors(saveChangesInterceptor);
            }

            // This is key. We need options as a singleton so that CreateDbContext()
            // calls from tests use the same options (and thus the same connection string).
            services.AddSingleton(dbContextOptionsBuilder.Options);

            services.AddDbContextFactory<GameStoreContext>();

            services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<TestAuthOptions, TestAuthHandler>(
                        "TestScheme",
                         options =>
                         {
                             options.AuthenticationSucceeds = authenticationSucceeds;
                             options.UserId = userId ?? Guid.NewGuid().ToString();
                             options.Role = role;
                             options.Scope = scope;
                         });
        });

        // Replace production services with test stubs after all other services are configured
        builder.ConfigureTestServices(services =>
        {
            AddBlobServiceClient(services, azuriteContainer);
            AddSessionServiceStub(services);
            AddPaymentMethodServiceStub(services);
            AddPaymentIntentServiceStub(services);
            AddMessagePublisherStub(services, messagePublisher);
            AddStripeEventFactoryStub(services);
        });

        // This prevents DirectoryNotFoundException when running tests with only compiled DLLs
        builder.UseContentRoot(Directory.GetCurrentDirectory());
    }

    private static void AddBlobServiceClient(
            IServiceCollection services,
            AzuriteContainer? azuriteContainer)
    {
        if (azuriteContainer is not null)
        {
            var blobServiceClient = new BlobServiceClient(azuriteContainer.GetConnectionString());
            services.RemoveAll(typeof(BlobServiceClient));
            services.AddSingleton(blobServiceClient);
        }
    }

    private static void AddSessionServiceStub(IServiceCollection services)
    {
        var session = new Session
        {
            Id = "cs_test_session",
            ClientSecret = "cs_test_secret"
        };
        var sessionService = Substitute.For<SessionService>();
        sessionService.CreateAsync(
            Arg.Any<SessionCreateOptions>(),
            Arg.Any<RequestOptions>(),
            default)
            .Returns(Task.FromResult(session));
        services.RemoveAll(typeof(SessionService));
        services.AddSingleton(sessionService);
    }

    private static void AddPaymentMethodServiceStub(IServiceCollection services)
    {
        var paymentMethod = new PaymentMethod
        {
            Id = "pm_test",
            Card = new PaymentMethodCard { Brand = "visa", Last4 = "4242" }
        };
        var paymentMethodService = Substitute.For<PaymentMethodService>();
        paymentMethodService.GetAsync(
            Arg.Any<string>(),
            Arg.Any<PaymentMethodGetOptions>(),
            Arg.Any<RequestOptions>(),
            default)
            .Returns(Task.FromResult(paymentMethod));
        services.RemoveAll(typeof(PaymentMethodService));
        services.AddSingleton(paymentMethodService);
    }

    private static void AddPaymentIntentServiceStub(IServiceCollection services)
    {
        var paymentMethod = new PaymentMethod
        {
            Id = "pm_test",
            Card = new PaymentMethodCard { Brand = "visa", Last4 = "4242" }
        };
        var paymentIntent = new PaymentIntent
        {
            Id = "pi_test",
            PaymentMethod = paymentMethod
        };
        var paymentIntentService = Substitute.For<PaymentIntentService>();
        paymentIntentService.GetAsync(
            Arg.Any<string>(),
            Arg.Any<PaymentIntentGetOptions>(),
            Arg.Any<RequestOptions>(),
            default)
            .Returns(Task.FromResult(paymentIntent));
        services.RemoveAll(typeof(PaymentIntentService));
        services.AddSingleton(paymentIntentService);
    }

    private static void AddMessagePublisherStub(
        IServiceCollection services,
        IMessagePublisher? messagePublisher)
    {
        services.RemoveAll(typeof(IMessagePublisher));

        messagePublisher ??= Substitute.For<IMessagePublisher>();

        services.AddSingleton(messagePublisher);
    }

    private void AddStripeEventFactoryStub(IServiceCollection services)
    {
        if (stripeEventFactoryOverride is not null)
        {
            services.RemoveAll(typeof(IStripeEventFactory));
            services.AddSingleton(stripeEventFactoryOverride);
        }
    }

    private static void SeedGenres(DbContext context)
    {
        var fixture = new Fixture();

        fixture.Customize<Genre>(composer => composer.With(
            g => g.Name, fixture.Create<string>()[..20]));

        var genres = fixture.CreateMany<Genre>();

        context.Set<Genre>().AddRange(genres);
    }
}