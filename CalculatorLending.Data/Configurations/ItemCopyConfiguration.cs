using CalculatorLending.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalculatorLending.Data.Configurations;

public class ItemCopyConfiguration : IEntityTypeConfiguration<ItemCopy>
{
    public void Configure(EntityTypeBuilder<ItemCopy> builder)
    {
        builder.Property(c => c.SerialNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.SerialNumber).IsUnique();

        // Status is stored as int (EF default for enums)

        // Npgsql maps a uint row version to the xmin system column
        builder.Property(c => c.Version).IsRowVersion();
    }
}
