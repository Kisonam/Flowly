using System.ComponentModel.DataAnnotations;

namespace Flowly.Application.DTOs.Tasks;

public class ReorderTasksDto
{
    [Required]
    public List<TaskOrderItemDto> Items { get; set; } = new();
}

public class TaskOrderItemDto
{
    [Required]
    public Guid TaskId { get; set; }
    public Guid? ThemeId { get; set; }
    public int Order { get; set; }
}
