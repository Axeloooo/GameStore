using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using GameStore.Api.Shared.Stripe;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameStore.Api.UnitTests.Shared.Stripe;

// StripeEventFactory is internal to GameStore.Api; these tests compile only
// because GameStore.Api.csproj declares
// <InternalsVisibleTo Include="GameStore.Api.UnitTests" />.
// Signatures are computed locally with a dummy secret: no Stripe account,
// network access or real webhook secret is involved.
public class StripeEventFactoryTests
{
    private const string EndpointSecret = "unit-test-endpoint-secret";

    // Older API version on purpose: Stripe sends events in the account's
    // default version, which rarely matches the version Stripe.net pins.
    private const string EventJson = """
        {
          "id": "evt_unit_test_1",
          "object": "event",
          "api_version": "2022-11-15",
          "created": 1759752000,
          "livemode": false,
          "pending_webhooks": 1,
          "request": { "id": null, "idempotency_key": null },
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "cs_unit_test_1",
              "object": "checkout.session",
              "metadata": { "OrderId": "3f1d2a5e-1111-4c3b-9a0e-6b1f2c3d4e5f" }
            }
          }
        }
        """;

    private static StripeEventFactory CreateSut() => new(Options.Create(new StripeOptions
    {
        SecretKey = "unused-in-these-tests",
        CheckoutReturnUrl = "https://localhost/orders",
        EndpointSecret = EndpointSecret
    }));

    private static string Sign(string payload, string secret, DateTimeOffset timestamp)
    {
        var unixTime = timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{unixTime}.{payload}"));

        return $"t={unixTime},v1={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingBody_ThrowsArgumentExceptionForBody(string? body)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Create(body!, "t=1,v1=abc");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithParameterName("jsonBody")
           .WithMessage("Request body is required.*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingSignatureHeader_ThrowsArgumentExceptionForHeader(string? signatureHeader)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Create(EventJson, signatureHeader!);

        // Assert
        act.Should().ThrowExactly<ArgumentException>()
           .WithParameterName("signatureHeader");
    }

    [Fact]
    public void Create_ValidSignatureAndOlderApiVersion_ReturnsTheParsedEvent()
    {
        // Arrange
        var sut = CreateSut();
        var signature = Sign(EventJson, EndpointSecret, DateTimeOffset.UtcNow);

        // Act
        var stripeEvent = sut.Create(EventJson, signature);

        // Assert
        stripeEvent.Id.Should().Be("evt_unit_test_1");
        stripeEvent.Type.Should().Be(EventTypes.CheckoutSessionCompleted);
        stripeEvent.Data.Object.Should().BeOfType<Session>()
                   .Which.Metadata.Should().ContainKey("OrderId");
    }

    [Fact]
    public void Create_SignedWithAnotherSecret_ThrowsStripeException()
    {
        // Arrange
        var sut = CreateSut();
        var signature = Sign(EventJson, "some-other-secret", DateTimeOffset.UtcNow);

        // Act
        var act = () => sut.Create(EventJson, signature);

        // Assert
        act.Should().Throw<StripeException>();
    }

    [Fact]
    public void Create_TamperedBody_ThrowsStripeException()
    {
        // Arrange
        var sut = CreateSut();
        var signature = Sign(EventJson, EndpointSecret, DateTimeOffset.UtcNow);
        var tamperedJson = EventJson.Replace("cs_unit_test_1", "cs_attacker");

        // Act
        var act = () => sut.Create(tamperedJson, signature);

        // Assert
        act.Should().Throw<StripeException>();
    }
}
