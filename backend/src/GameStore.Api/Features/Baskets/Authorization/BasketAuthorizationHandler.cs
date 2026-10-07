using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameStore.Data.Models;
using GameStore.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace GameStore.Api.Features.Baskets.Authorization;

// Authorization handler. A class responsible for the evaluation of
// a requirement's properties.

// A resource-based handler is an authorization handler that specifies
// both a requirement and a resource type.

public class BasketAuthorizationHandler
    : AuthorizationHandler<OwnerOrAdminRequirement, CustomerBasket>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerOrAdminRequirement requirement,
        CustomerBasket resource)
    {
        var currentUserId = context.User.FindFirstValue(GameStoreClaimTypes.UserId);

        if (string.IsNullOrEmpty(currentUserId))
        {
            return Task.CompletedTask;
        }

        if (Guid.Parse(currentUserId) == resource.Id
            || context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Authorization requirement. A collection of data parameters that a policy
// can use to evaluate the current user principal.
public class OwnerOrAdminRequirement : IAuthorizationRequirement { }