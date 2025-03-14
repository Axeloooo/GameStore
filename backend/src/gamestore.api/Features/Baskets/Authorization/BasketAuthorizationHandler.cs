using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using gamestore.api.Models;
using gamestore.api.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace gamestore.api.Features.Baskets.Authorization;

public class BasketAuthorizationHandler
    : AuthorizationHandler<OwnerOrAdminRequirement, CustomerBasket>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerOrAdminRequirement requirement,
        CustomerBasket resource
    )
    {
        var currentUserid = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(currentUserid))
        {
            return Task.CompletedTask;
        }

        if (Guid.Parse(currentUserid) == resource.Id || context.User.IsInRole(Roles.Admin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class OwnerOrAdminRequirement : IAuthorizationRequirement { }
