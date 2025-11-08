using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Description).HasColumnType("text");
        builder.Property(t => t.Color).HasMaxLength(7);
        builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Priority).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.IsArchived).HasDefaultValue(false);
        builder.Property(t => t.Order).IsRequired();

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.TaskThemeId);
        builder.HasIndex(t => new { t.TaskThemeId, t.Order });
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.DueDate);
        builder.HasIndex(t => t.IsArchived);

        builder.HasOne(t => t.TaskTheme)
            .WithMany(th => th.Tasks)
            .HasForeignKey(t => t.TaskThemeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Subtasks)
            .WithOne(s => s.TaskItem)
            .HasForeignKey(s => s.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Recurrence)
            .WithOne(r => r.TaskItem)
            .HasForeignKey<TaskRecurrence>(r => r.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
