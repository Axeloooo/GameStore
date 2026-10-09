using System.Security.Claims;
using FluentAssertions;
using GameStore.Api.Shared.Authorization;

namespace GameStore.Api.UnitTests.Shared.Authorization;

public class ClaimsExtensionsTests
{
    private const string EntraScopeClaimType = "scp";

    // ClassData: the scenarios live in their own reusable type.
    public sealed class ScopeClaimScenarios : TheoryData<string, string[]>
    {
        public ScopeClaimScenarios()
        {
            Add("games:read", ["games:read"]);
            Add("games:read games:write", ["games:read", "games:write"]);
            Add("openid profile games:read", ["openid", "profile", "games:read"]);
        }
    }

    [Theory]
    [ClassData(typeof(ScopeClaimScenarios))]
    public void TransformScopeClaim_SpaceSeparatedScopes_AddsOneScopeClaimPerScope(
        string scopeClaimValue,
        string[] expectedScopes)
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim(EntraScopeClaimType, scopeClaimValue)]);

        // Act
        identity.TransformScopeClaim(EntraScopeClaimType);

        // Assert
        identity.FindAll(GameStoreClaimTypes.Scope)
                .Select(claim => claim.Value)
                .Should().Equal(expectedScopes);
        identity.HasClaim(claim => claim.Type == EntraScopeClaimType)
                .Should().BeFalse("the source claim is replaced, not duplicated");
    }

    [Fact]
    public void TransformScopeClaim_NoSourceClaim_LeavesIdentityUnchanged()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim("name", "alice")]);

        // Act
        identity.TransformScopeClaim(EntraScopeClaimType);

        // Assert
        identity.Claims.Should().ContainSingle()
                .Which.Value.Should().Be("alice");
    }

    [Fact]
    public void TransformScopeClaim_NullIdentity_DoesNotThrow()
    {
        // Arrange
        ClaimsIdentity? identity = null;

        // Act
        var act = () => identity.TransformScopeClaim(EntraScopeClaimType);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("oid", "5e2b8c1d-7a3f-4e9b-b6c2-1d0f8a7e3b4c")]
    [InlineData("sub", "keycloak-user-42")]
    public void MapUserIdClaim_SourceClaimPresent_AddsUserIdClaimWithSameValue(string sourceType, string value)
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim(sourceType, value)]);

        // Act
        identity.MapUserIdClaim(sourceType);

        // Assert
        identity.FindFirst(GameStoreClaimTypes.UserId)!.Value.Should().Be(value);
        identity.FindFirst(sourceType).Should().NotBeNull("the source claim is kept");
    }

    [Fact]
    public void MapUserIdClaim_SourceClaimMissing_AddsNoUserIdClaim()
    {
        // Arrange
        var identity = new ClaimsIdentity([new Claim("email", "alice@example.test")]);

        // Act
        identity.MapUserIdClaim("oid");

        // Assert
        identity.FindFirst(GameStoreClaimTypes.UserId).Should().BeNull();
    }
}
