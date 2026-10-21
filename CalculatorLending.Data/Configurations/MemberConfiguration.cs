using CalculatorLending.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalculatorLending.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.Property(m => m.FullName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Email).IsRequired().HasMaxLength(254);
        builder.HasIndex(m => m.Email).IsUnique();
        builder.Property(m => m.PasswordHash).IsRequired().HasMaxLength(200);

        // Role is stored as int (EF default for enums)
    }
}
