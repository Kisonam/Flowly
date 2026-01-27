

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Flowly.Domain.Entities;

namespace Flowly.Infrastructure.Data.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.PeriodStart)
            .IsRequired();

        builder.Property(b => b.PeriodEnd)
            .IsRequired();

        builder.Property(b => b.Limit)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(b => b.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.CategoryId);
        builder.HasIndex(b => new { b.UserId, b.PeriodStart, b.PeriodEnd });

        builder.HasOne(b => b.Currency)
            .WithMany()
            .HasForeignKey(b => b.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}