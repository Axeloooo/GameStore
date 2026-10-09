using System.Security.Claims;
using FluentAssertions;
using GameStore.Api.Features.Baskets.Authorization;
using GameStore.Api.Shared.Authorization;
using GameStore.Data.Models;
using Microsoft.AspNetCore.Authorization;

namespace GameStore.Api.UnitTests.Features.Baskets;

public class BasketAuthorizationHandlerTests
{
    private static readonly Guid OwnerId = Guid.Parse("6f1c2c43-5d0e-4a35-9f43-1f3a0c9b8e21");
    private static readonly Guid OtherUserId = Guid.Parse("0b7e6f2a-91d4-4c1e-8a57-3d2e9c4f6a10");

    private static ClaimsPrincipal CreateUser(Guid? userId, bool isAdmin)
    {
        var claims = new List<Claim>();

        if (userId is not null)
        {
            claims.Add(new Claim(GameStoreClaimTypes.UserId, userId.Value.ToString()));
        }

        if (isAdmin)
        {
            claims.Add(new Claim(GameStoreClaimTypes.Role, Roles.Admin));
        }

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "UnitTest",
            nameType: null,
            roleType: GameStoreClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }

    private static async Task<AuthorizationHandlerContext> AuthorizeAsync(ClaimsPrincipal user, CustomerBasket basket)
    {
        var requirement = new OwnerOrAdminRequirement();
        var context = new AuthorizationHandlerContext([requirement], user, basket);
        var sut = new BasketAuthorizationHandler();

        await sut.HandleAsync(context);

        return context;
    }

    [Theory]
    [InlineData("owner", false, true)]
    [InlineData("owner", true, true)]
    [InlineData("other", true, true)]
    [InlineData("other", false, false)]
    [InlineData("anonymous", true, false)]
    [InlineData("anonymous", false, false)]
    public async Task HandleAsync_UserAndOwnerCombination_SucceedsOnlyForOwnerOrAdmin(
        string caller,
        bool isAdmin,
        bool expectedSuccess)
    {
        // Arrange
        Guid? callerId = caller switch
        {
            "owner" => OwnerId,
            "other" => OtherUserId,
            _ => null // no userId claim at all
        };
        var user = CreateUser(callerId, isAdmin);
        var basket = new CustomerBasket { Id = OwnerId };

        // Act
        var context = await AuthorizeAsync(user, basket);

        // Assert
        context.HasSucceeded.Should().Be(expectedSuccess);
        context.HasFailed.Should().BeFalse("the handler never forces a failure, it only abstains");
    }

    [Fact]
    public async Task HandleAsync_OwnerOfBasket_MarksRequirementAsSatisfied()
    {
        // Arrange
        var user = CreateUser(OwnerId, isAdmin: false);
        var basket = new CustomerBasket { Id = OwnerId };

        // Act
        var context = await AuthorizeAsync(user, basket);

        // Assert
        context.HasSucceeded.Should().BeTrue();
        context.PendingRequirements.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_AdminWithoutUserIdClaim_LeavesRequirementPending()
    {
        // Arrange: being an admin is not enough, the user must be identified.
        var user = CreateUser(userId: null, isAdmin: true);
        var basket = new CustomerBasket { Id = OwnerId };

        // Act
        var context = await AuthorizeAsync(user, basket);

        // Assert
        context.HasSucceeded.Should().BeFalse();
        context.PendingRequirements.Should().ContainSingle()
               .Which.Should().BeOfType<OwnerOrAdminRequirement>();
    }
}
