using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
     public void Configure(EntityTypeBuilder<Note> builder)
    {
        // Table name
        builder.ToTable("Notes");

        // Primary key
        builder.HasKey(n => n.Id);

        // Properties
        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Markdown)
            .IsRequired();

        builder.Property(n => n.HtmlCache)
            .HasColumnType("text");

        builder.Property(n => n.IsArchived)
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.IsArchived);
        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => new { n.UserId, n.IsArchived }); // Composite for filtering

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notes)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(n => n.MediaAssets)
            .WithOne(m => m.Note)
            .HasForeignKey(m => m.NoteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
