using GameStore.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameStore.Data.Configurations;

public class OrderEntityConfiguration : IEntityTypeConfiguration<Order>
{
    public const string OrderNumbersSequence = "OrderNumbers";

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(order => order.OrderNumber)
                .HasDefaultValueSql($"nextval('\"{OrderNumbersSequence}\"')");

        builder.Property(order => order.Status)
                .HasConversion<string>();

        builder.Property(order => order.TotalAmount)
                .HasPrecision(18, 2);

        builder.HasIndex(order => new { order.CustomerId, order.OperationId })
                .IsUnique();
    }
}
