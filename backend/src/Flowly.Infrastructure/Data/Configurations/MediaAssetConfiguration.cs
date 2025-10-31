using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
     public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Path).IsRequired().HasMaxLength(500);
        builder.Property(m => m.FileName).IsRequired().HasMaxLength(255);
        builder.Property(m => m.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Size).IsRequired();

        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.NoteId);
        builder.HasIndex(m => m.CreatedAt);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
