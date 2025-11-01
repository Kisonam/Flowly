using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Flowly.Domain.Entities;

namespace Flowly.Infrastructure.Data.Configurations;

public class ArchiveEntryConfiguration : IEntityTypeConfiguration<ArchiveEntry>
{
    public void Configure(EntityTypeBuilder<ArchiveEntry> builder)
    {
        builder.ToTable("ArchiveEntries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.PayloadJson)
            .IsRequired()
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.ArchivedAt);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}