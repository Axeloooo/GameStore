using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;

namespace GameStore.Api.Shared.Authorization;

public static class AuthorizationExtensions
{
    private const string ApiAccessScope = "gamestore_api.all";

    public static IHostApplicationBuilder AddGameStoreAuthentication(
        this IHostApplicationBuilder builder)
    {
        var authBuilder = builder.Services.AddAuthentication(Schemes.KeyCloakOrEntra);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<KeycloakClaimsTransformer>();

            authBuilder.AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.RoleClaimType = GameStoreClaimTypes.Role;
            })
            .AddJwtBearer(Schemes.Keycloak, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.RoleClaimType = GameStoreClaimTypes.Role;
                options.RequireHttpsMetadata = false;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var transformer = context.HttpContext
                                                    .RequestServices
                                                    .GetRequiredService<KeycloakClaimsTransformer>();
                        transformer.Transform(context);

                        return Task.CompletedTask;
                    }
                };
            });
        }

        builder.Services.AddSingleton<EntraClaimsTransformer>();

        authBuilder.AddJwtBearer(Schemes.Entra, options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters.RoleClaimType = GameStoreClaimTypes.Roles;

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var transformer = context.HttpContext
                                            .RequestServices
                                            .GetRequiredService<EntraClaimsTransformer>();
                    transformer.Transform(context);

                    return Task.CompletedTask;
                }
            };
        });

        authBuilder.AddPolicyScheme(
            Schemes.KeyCloakOrEntra,
            Schemes.KeyCloakOrEntra,
            options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    string authorization = context.Request.Headers[HeaderNames.Authorization]!;

                    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                    {
                        var token = authorization["Bearer ".Length..].Trim();

                        var jwtHandler = new JwtSecurityTokenHandler();

                        return jwtHandler.CanReadToken(token) &&
                                jwtHandler.ReadJwtToken(token)
                                    .Issuer.Contains("ciamlogin.com")
                                        ? Schemes.Entra : Schemes.Keycloak;
                    }

                    return Schemes.Entra;
                };
            });

        return builder;
    }

    public static IHostApplicationBuilder AddGameStoreAuthorization(
        this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthorizationBuilder()
                        .AddFallbackPolicy(Policies.UserAccess, authBuilder =>
                        {
                            authBuilder.RequireClaim(GameStoreClaimTypes.Scope, ApiAccessScope);
                        })
                        .AddPolicy(Policies.AdminAccess, authBuilder =>
                        {
                            authBuilder.RequireClaim(GameStoreClaimTypes.Scope, ApiAccessScope);
                            authBuilder.RequireRole(Roles.Admin);
                        });

        return builder;
    }
}
