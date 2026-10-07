using Microsoft.AspNetCore.Authentication;

namespace GameStore.IntegrationTests.Authentication;

public class TestAuthOptions : AuthenticationSchemeOptions
{
    public bool AuthenticationSucceeds { get; set; }
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public string? Scope { get; set; }
}

