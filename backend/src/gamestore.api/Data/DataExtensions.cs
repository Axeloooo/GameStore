using System.Threading.Tasks;
using gamestore.api.Models;
using Microsoft.EntityFrameworkCore;

namespace gamestore.api.Data;

public static class DataExtensions
{
    public static async Task InitializeDbAsync(this WebApplication app)
    {
        await app.MigrateDbAsync();
        await app.SeedDbAsync();
    }

    private static async Task MigrateDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        GameStoreContext DbContext = scope.ServiceProvider.GetRequiredService<GameStoreContext>();
        await DbContext.Database.MigrateAsync();
    }

    private static async Task SeedDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        GameStoreContext DbContext = scope.ServiceProvider.GetRequiredService<GameStoreContext>();

        if (!DbContext.Genres.Any())
        {
            DbContext.AddRange(
                new Genre { Name = "Fighting" },
                new Genre { Name = "Roleplaying" },
                new Genre { Name = "Sports" },
                new Genre { Name = "Kids and Family" },
                new Genre { Name = "Racing" }
            );
            await DbContext.SaveChangesAsync();
        }
    }
}
