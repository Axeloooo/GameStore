using GameStore.Data.Configurations;
using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Data;

public class GameStoreContext(DbContextOptions<GameStoreContext> options)
    : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();

    public DbSet<Genre> Genres => Set<Genre>();

    public DbSet<CustomerBasket> Baskets => Set<CustomerBasket>();

    public DbSet<BasketItem> BasketItems => Set<BasketItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(OrderEntityConfiguration.OrderNumbersSequence)
                    .StartsAt(1)
                    .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameEntityConfiguration).Assembly);
    }
}
