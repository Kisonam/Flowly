using System;

namespace Flowly.Application.DTOs.Tasks;

public class TaskListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
}
