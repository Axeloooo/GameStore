using Azure.Core;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace GameStore.Data;

public static class DataExtensions
{
    public static IHostApplicationBuilder AddGameStoreNpgsql<TContext>(
            this IHostApplicationBuilder builder,
            string connectionStringName,
            TokenCredential credential
        ) where TContext : DbContext
    {
        if (builder.Environment.IsProduction())
        {
            builder.AddAzureNpgsqlDbContext<TContext>(
                connectionStringName,
                settings => settings.Credential = credential,
                configureDbContextOptions: options => UseSeeding(options)
            );
        }
        else
        {
            builder.AddNpgsqlDbContext<TContext>(
                connectionStringName,
                configureDbContextOptions: options => UseSeeding(options));
        }

        return builder;
    }

    private static DbContextOptionsBuilder UseSeeding(DbContextOptionsBuilder options)
    {
        return options.UseSeeding((context, _) =>
        {
            if (!context.Set<Genre>().Any())
            {
                SeedGenres(context);
                context.SaveChanges();
            }
        })
        .UseAsyncSeeding(async (context, _, cancellationToken) =>
        {
            if (!context.Set<Genre>().Any())
            {
                SeedGenres(context);
                await context.SaveChangesAsync(cancellationToken);
            }
        });
    }

    private static void SeedGenres(DbContext context)
    {
        context.Set<Genre>().AddRange(
            new Genre { Name = "Fighting" },
            new Genre { Name = "Kids and Family" },
            new Genre { Name = "Racing" },
            new Genre { Name = "Roleplaying" },
            new Genre { Name = "Sports" }
        );
    }
}
