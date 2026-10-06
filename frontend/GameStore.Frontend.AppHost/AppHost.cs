var builder = DistributedApplication.CreateBuilder(args);

const string BackendApiUrlKey = "VITE_BACKEND_API_URL";
const string IdentityProviderKey = "VITE_IDENTITY_PROVIDER";
const string EntraClientIdKey = "VITE_ENTRA_CLIENT_ID";
const string EntraAuthorityKey = "VITE_ENTRA_AUTHORITY";
const string EntraScopeKey = "VITE_ENTRA_SCOPE";

var backendUrl = builder.AddParameter("BackendUrl");
var entraClientId = builder.AddParameter("EntraClientId");
var entraAuthority = builder.AddParameter("EntraAuthority");
var entraScope = builder.AddParameter("EntraScope");

var identityProvider = builder.Configuration["IdentityProvider"]
    ?? throw new InvalidOperationException("IdentityProvider is not set in the configuration.");

var frontend = builder.AddNpmApp("gamestore-frontend", "../GameStore.Frontend", "dev")
                    .WithHttpEndpoint(port: 5173, env: "VITE_PORT")
                    .WithExternalHttpEndpoints()
                    .PublishAsDockerFile(container =>
                    {
                        container.WithBuildArg(BackendApiUrlKey, backendUrl)
                                .WithBuildArg(IdentityProviderKey, identityProvider)
                                .WithBuildArg(EntraClientIdKey, entraClientId)
                                .WithBuildArg(EntraAuthorityKey, entraAuthority)
                                .WithBuildArg(EntraScopeKey, entraScope);
                    });

if (builder.ExecutionContext.IsRunMode)
{
    frontend.WithEnvironment(BackendApiUrlKey, backendUrl)
            .WithEnvironment(IdentityProviderKey, identityProvider)
            .WithEnvironment(EntraClientIdKey, entraClientId)
            .WithEnvironment(EntraAuthorityKey, entraAuthority)
            .WithEnvironment(EntraScopeKey, entraScope);

    if (identityProvider == "Keycloak")
    {
        var keycloakClientId = builder.AddParameter("KeycloakClientId");
        var keycloakAuthority = builder.AddParameter("KeycloakAuthority");
        var keycloakScope = builder.AddParameter("KeycloakScope");

        frontend.WithEnvironment("VITE_KEYCLOAK_CLIENT_ID", keycloakClientId)
                .WithEnvironment("VITE_KEYCLOAK_AUTHORITY", keycloakAuthority)
                .WithEnvironment("VITE_KEYCLOAK_SCOPE", keycloakScope);
    }
}

builder.Build().Run();
