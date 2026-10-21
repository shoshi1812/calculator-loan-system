using CalculatorLending.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalculatorLending.Data.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        // Computed property, not stored
        builder.Ignore(l => l.IsActive);

        // Restrict: loan history is never deleted
        builder.HasOne(l => l.Member)
            .WithMany(m => m.Loans)
            .HasForeignKey(l => l.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ItemCopy)
            .WithMany(c => c.Loans)
            .HasForeignKey(l => l.ItemCopyId)
            .OnDelete(DeleteBehavior.Restrict);
        // Speeds up counting a member's active loans
        builder.HasIndex(l => new { l.MemberId, l.ReturnDate });

        // Second line of defense: at most one active loan per copy
        builder.HasIndex(l => l.ItemCopyId)
            .IsUnique()
            .HasFilter("\"ReturnDate\" IS NULL");
    }
}
