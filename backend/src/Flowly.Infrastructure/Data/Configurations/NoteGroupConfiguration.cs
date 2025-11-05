using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class NoteGroupConfiguration : IEntityTypeConfiguration<NoteGroup>
{
    public void Configure(EntityTypeBuilder<NoteGroup> builder)
    {
        builder.ToTable("NoteGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Order)
            .HasDefaultValue(0);

        builder.Property(g => g.Color)
            .HasMaxLength(20);

        builder.HasIndex(g => new { g.UserId, g.Order });

        builder.HasMany(g => g.Notes)
            .WithOne(n => n.NoteGroup)
            .HasForeignKey(n => n.NoteGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
