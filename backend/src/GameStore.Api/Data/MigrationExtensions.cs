using GameStore.Data;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Data;

public static class MigrationExtensions
{
    public static async Task MigrateDbAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        GameStoreContext dbContext = scope.ServiceProvider
                                          .GetRequiredService<GameStoreContext>();
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation(18, "The Game Store database is ready!");
    }
}
