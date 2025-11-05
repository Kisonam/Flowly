using Flowly.Application.DTOs.Tasks;
using Flowly.Application.Interfaces;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class TaskItemQueryService : ITaskItemQueryService
{
    private readonly AppDbContext _dbContext;

    public TaskItemQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TaskListItemDto>> GetListAsync(Guid userId, string? search = null, bool? isArchived = null, int take = 50)
    {
        if (take < 1) take = 1;
        if (take > 100) take = 100;

        var query = _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(term));
        }

        if (isArchived.HasValue)
        {
            query = query.Where(t => t.IsArchived == isArchived.Value);
        }

        return await query
            .OrderByDescending(t => t.UpdatedAt)
            .Take(take)
            .Select(t => new TaskListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                IsArchived = t.IsArchived
            })
            .ToListAsync();
    }
}
