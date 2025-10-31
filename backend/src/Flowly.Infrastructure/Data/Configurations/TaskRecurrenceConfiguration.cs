using System;
using Flowly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flowly.Infrastructure.Data.Configurations;

public class TaskRecurrenceConfiguration : IEntityTypeConfiguration<TaskRecurrence>
{
    public void Configure(EntityTypeBuilder<TaskRecurrence> builder)
    {
        builder.ToTable("TaskRecurrences");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rule).IsRequired().HasColumnType("text");

        builder.HasIndex(r => r.TaskItemId).IsUnique();
    }
}
