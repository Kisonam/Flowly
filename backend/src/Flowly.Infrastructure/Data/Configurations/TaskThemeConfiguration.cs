using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class TaskThemeConfiguration: IEntityTypeConfiguration<TaskTheme>
{
    public void Configure(EntityTypeBuilder<TaskTheme> builder)
    {
        builder.ToTable("TaskThemes");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Order).IsRequired();
        builder.Property(t => t.Color).HasMaxLength(7);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => new { t.UserId, t.Order });
    }
}