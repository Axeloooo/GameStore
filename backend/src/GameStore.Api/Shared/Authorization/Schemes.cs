namespace GameStore.Api.Shared.Authorization;

public static class Schemes
{
    public const string Keycloak = nameof(Keycloak);
    public const string Entra = nameof(Entra);
    public const string KeyCloakOrEntra = nameof(KeyCloakOrEntra);
}
