using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStore.Api.Data.Configurations;

public class GameEntityConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(game => game.Name)
               .HasMaxLength(50);

        builder.Property(game => game.Description)
               .HasMaxLength(500);

        builder.Property(game => game.Price)
               .HasPrecision(5, 2);
    }
}
