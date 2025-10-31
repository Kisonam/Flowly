using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class LinkConfiguration : IEntityTypeConfiguration<Link>
{
     public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.ToTable("Links");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.FromType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ToType).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => new { l.FromType, l.FromId });
        builder.HasIndex(l => new { l.ToType, l.ToId });
        builder.HasIndex(l => new { l.FromType, l.FromId, l.ToType, l.ToId }).IsUnique();
    }
}
