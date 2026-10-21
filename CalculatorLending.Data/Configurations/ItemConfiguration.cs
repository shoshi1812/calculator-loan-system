using CalculatorLending.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalculatorLending.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name).IsRequired().HasMaxLength(150);
        builder.Property(i => i.ModelNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.ModelNumber).IsUnique();
        builder.Property(i => i.Description).HasMaxLength(1000);

        // EF creates the join table on its own
        builder.HasMany(i => i.Categories)
            .WithMany(c => c.Items)
            .UsingEntity(j => j.ToTable("ItemCategories"));

        builder.HasMany(i => i.Copies)
            .WithOne(c => c.Item)
            .HasForeignKey(c => c.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
