

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Flowly.Domain.Entities;

namespace Flowly.Infrastructure.Data.Configurations;

public class FinancialGoalConfiguration : IEntityTypeConfiguration<FinancialGoal>
{
    public void Configure(EntityTypeBuilder<FinancialGoal> builder)
    {
        builder.ToTable("FinancialGoals");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.TargetAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(g => g.CurrentAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(g => g.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(g => g.Description)
            .HasMaxLength(1000);

        builder.Property(g => g.IsArchived)
            .HasDefaultValue(false);

        builder.HasIndex(g => g.UserId);
        builder.HasIndex(g => g.Deadline);
        builder.HasIndex(g => g.IsArchived);

        builder.HasOne(g => g.Currency)
            .WithMany()
            .HasForeignKey(g => g.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}