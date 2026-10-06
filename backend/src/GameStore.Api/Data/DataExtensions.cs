using Azure.Core;
using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Data;

public static class DataExtensions
{
    private const string postgreSqlScope = "https://ossrdbms-aad.database.windows.net/.default";

    public static async Task InitializeDbAsync(this WebApplication app)
    {
        await app.MigrateDbAsync();
        await app.SeedDbAsync();
        app.Logger.LogInformation(18, "The database is ready!");
    }

    public static WebApplicationBuilder AddGameStoreNpgsql<TContext>(
        this WebApplicationBuilder builder,
        string connectionStringName,
        TokenCredential credential
    ) where TContext : DbContext
    {
        var connString = builder.Configuration.GetConnectionString(connectionStringName);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddNpgsql<TContext>(connString);
        }
        else
        {
            builder.Services.AddNpgsql<TContext>(connString, dbContextOptionsBuilder =>
            {
                dbContextOptionsBuilder.ConfigureDataSource(dataSourceBuilder =>
                {
                    dataSourceBuilder.UsePeriodicPasswordProvider(
                        async (_, cancellationToken) =>
                        {
                            var token = await credential.GetTokenAsync(
                                new TokenRequestContext([postgreSqlScope]),
                                cancellationToken
                            );

                            return token.Token;
                        },
                        TimeSpan.FromHours(24),
                        TimeSpan.FromSeconds(10)
                    );
                });
            });
        }

        return builder;
    }

    private static async Task MigrateDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        GameStoreContext dbContext = scope.ServiceProvider
                                          .GetRequiredService<GameStoreContext>();
        await dbContext.Database.MigrateAsync();
    }

    private static async Task SeedDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        GameStoreContext dbContext = scope.ServiceProvider
                                          .GetRequiredService<GameStoreContext>();

        if (!dbContext.Genres.Any())
        {
            dbContext.Genres.AddRange(
                new Genre { Name = "Fighting" },
                new Genre { Name = "Kids and Family" },
                new Genre { Name = "Racing" },
                new Genre { Name = "Roleplaying" },
                new Genre { Name = "Sports" }
            );

            await dbContext.SaveChangesAsync();
        }
    }
}
