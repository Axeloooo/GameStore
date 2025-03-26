using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace gamestore.api.Shared.Authorization;

public class KeycloakClaimsTransformer(ILogger<KeycloakClaimsTransformer> logger)
{
    public void Transform(TokenValidatedContext context)
    {
        ClaimsIdentity? identity = context.Principal?.Identity as ClaimsIdentity;

        Claim? scopeClaim = identity?.FindFirst(ClaimTypes.Scope);

        if (scopeClaim is null)
        {
            return;
        }

        string[] scopes = scopeClaim.Value.Split(" ");

        identity?.RemoveClaim(scopeClaim);

        identity?.AddClaims(scopes.Select(scope => new Claim(ClaimTypes.Scope, scope)));

        IEnumerable<Claim>? claims = context.Principal?.Claims;

        if (claims is null)
        {
            return;
        }

        foreach (var claim in claims)
        {
            logger.LogTrace("Claim: {ClaimType}, Value: {ClaimValue}", claim.Type, claim.Value);
        }
    }
}
