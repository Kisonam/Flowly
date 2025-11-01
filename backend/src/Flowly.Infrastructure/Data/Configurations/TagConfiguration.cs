using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
     public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Color).HasMaxLength(7);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
    }
}
