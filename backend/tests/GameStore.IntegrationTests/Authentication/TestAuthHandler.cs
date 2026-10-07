using System.Security.Claims;
using System.Text.Encodings.Web;
using GameStore.Api.Shared.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameStore.IntegrationTests.Authentication;

public class TestAuthHandler(
    IOptionsMonitor<TestAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.AuthenticationSucceeds)
        {
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed"));
        }

        List<Claim> claims = [];

        if (!string.IsNullOrWhiteSpace(Options.UserId))
        {
            claims.Add(new Claim(GameStoreClaimTypes.UserId, Options.UserId));
        }

        if (!string.IsNullOrWhiteSpace(Options.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, Options.Role));
        }

        if (!string.IsNullOrWhiteSpace(Options.Scope))
        {
            claims.Add(new Claim("scope", Options.Scope));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}