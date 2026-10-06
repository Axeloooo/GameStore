using Microsoft.Net.Http.Headers;

namespace GameStore.Api.Shared.Cors;

public static class CorsExtensions
{
    public static IHostApplicationBuilder AddGameStoreCors(this IHostApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                policy =>
                {
                    var originsString = builder.Configuration["AllowedOrigins"] ?? string.Empty;
                    var allowedOrigins = originsString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    policy.WithOrigins(allowedOrigins)
                          .WithHeaders(HeaderNames.Authorization, HeaderNames.ContentType)
                          .AllowAnyMethod();
                });
        });

        return builder;
    }
}