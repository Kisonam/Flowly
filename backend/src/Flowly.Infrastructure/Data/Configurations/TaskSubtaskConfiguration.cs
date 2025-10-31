using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class TaskSubtaskConfiguration : IEntityTypeConfiguration<TaskSubtask>
{
 public void Configure(EntityTypeBuilder<TaskSubtask> builder)
    {
        builder.ToTable("TaskSubtasks");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).IsRequired().HasMaxLength(500);
        builder.Property(s => s.IsDone).HasDefaultValue(false);
        builder.Property(s => s.Order).IsRequired();

        builder.HasIndex(s => s.TaskItemId);
        builder.HasIndex(s => new { s.TaskItemId, s.Order });
    }
}
