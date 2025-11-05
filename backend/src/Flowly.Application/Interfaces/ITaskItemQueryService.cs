using Flowly.Application.DTOs.Tasks;

namespace Flowly.Application.Interfaces;

public interface ITaskItemQueryService
{
    Task<List<TaskListItemDto>> GetListAsync(Guid userId, string? search = null, bool? isArchived = null, int take = 50);
}
