using Flowly.Application.DTOs.Transactions;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class FinancialGoalService : IFinancialGoalService
{
    private readonly AppDbContext _dbContext;
    private readonly IArchiveService _archiveService;

    public FinancialGoalService(AppDbContext dbContext, IArchiveService archiveService)
    {
        _dbContext = dbContext;
        _archiveService = archiveService;
    }

    public async Task<List<FinancialGoalDto>> GetAllAsync(Guid userId, GoalFilterDto? filter = null)
    {
        var query = _dbContext.FinancialGoals
            .AsNoTracking()
            .Where(g => g.UserId == userId);

        // Apply filters
        if (filter != null)
        {
            if (filter.IsCompleted.HasValue)
            {
                if (filter.IsCompleted.Value)
                {
                    query = query.Where(g => g.CurrentAmount >= g.TargetAmount);
                }
                else
                {
                    query = query.Where(g => g.CurrentAmount < g.TargetAmount);
                }
            }

            if (filter.IsArchived.HasValue)
            {
                query = query.Where(g => g.IsArchived == filter.IsArchived.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
            {
                query = query.Where(g => g.CurrencyCode == filter.CurrencyCode);
            }

            if (filter.DeadlineFrom.HasValue)
            {
                var dateFrom = DateTime.SpecifyKind(filter.DeadlineFrom.Value, DateTimeKind.Utc);
                query = query.Where(g => g.Deadline.HasValue && g.Deadline.Value >= dateFrom);
            }

            if (filter.DeadlineTo.HasValue)
            {
                var dateTo = DateTime.SpecifyKind(filter.DeadlineTo.Value, DateTimeKind.Utc);
                query = query.Where(g => g.Deadline.HasValue && g.Deadline.Value <= dateTo);
            }
        }

        var goals = await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return goals.Select(MapToDto).ToList();
    }

    public async Task<FinancialGoalDto> GetByIdAsync(Guid userId, Guid goalId)
    {
        var goal = await _dbContext.FinancialGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        return MapToDto(goal);
    }

    public async Task<FinancialGoalDto> CreateAsync(Guid userId, CreateGoalDto dto)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

        if (dto.TargetAmount <= 0)
        {
            throw new ArgumentException("Target amount must be positive", nameof(dto.TargetAmount));
        }

        // Verify currency
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
        }

        var goal = new FinancialGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title.Trim(),
            TargetAmount = dto.TargetAmount,
            CurrencyCode = dto.CurrencyCode,
            CurrentAmount = 0,
            Deadline = dto.Deadline.HasValue 
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc) 
                : null,
            Description = dto.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.FinancialGoals.Add(goal);
        await _dbContext.SaveChangesAsync();

        return MapToDto(goal);
    }

    public async Task<FinancialGoalDto> UpdateAsync(Guid userId, Guid goalId, UpdateGoalDto dto)
    {
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        // Verify currency
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
        }

        goal.Update(
            dto.Title,
            dto.TargetAmount,
            dto.CurrencyCode,
            dto.Deadline.HasValue 
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc) 
                : null,
            dto.Description?.Trim()
        );

        await _dbContext.SaveChangesAsync();

        return MapToDto(goal);
    }

    public async Task DeleteAsync(Guid userId, Guid goalId)
    {
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        _dbContext.FinancialGoals.Remove(goal);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ArchiveAsync(Guid userId, Guid goalId)
    {
        await _archiveService.ArchiveEntityAsync(userId, LinkEntityType.FinancialGoal, goalId);
    }

    public async Task RestoreAsync(Guid userId, Guid goalId)
    {
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        goal.Restore();
        await _dbContext.SaveChangesAsync();
    }

    // ============================================
    // Progress Management
    // ============================================

    public async Task<FinancialGoalDto> AddAmountAsync(Guid userId, Guid goalId, UpdateGoalAmountDto dto)
    {
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        goal.AddAmount(dto.Amount);
        await _dbContext.SaveChangesAsync();

        return MapToDto(goal);
    }

    public async Task<FinancialGoalDto> SubtractAmountAsync(Guid userId, Guid goalId, UpdateGoalAmountDto dto)
    {
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        goal.SubtractAmount(dto.Amount);
        await _dbContext.SaveChangesAsync();

        return MapToDto(goal);
    }

    public async Task<FinancialGoalDto> SetCurrentAmountAsync(Guid userId, Guid goalId, UpdateGoalAmountDto dto)
    {
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        goal.SetCurrentAmount(dto.Amount);
        await _dbContext.SaveChangesAsync();

        return MapToDto(goal);
    }

    public async Task<List<TransactionListItemDto>> GetGoalTransactionsAsync(Guid userId, Guid goalId)
    {
        // Verify goal exists and belongs to user
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            throw new InvalidOperationException("Financial goal not found");
        }

        // Get all transactions linked to this goal
        var transactions = await _dbContext.Transactions
            .Include(t => t.Category)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => t.UserId == userId && t.GoalId == goalId)
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionListItemDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                CurrencyCode = t.CurrencyCode,
                Category = t.Category != null ? new CategoryDto
                {
                    Id = t.Category.Id,
                    Name = t.Category.Name,
                    UserId = t.Category.UserId
                } : null,
                Date = t.Date,
                Description = t.Description,
                BudgetId = t.BudgetId,
                GoalId = t.GoalId,
                CreatedAt = t.CreatedAt,
                Tags = t.TransactionTags.Select(tt => new TagDto
                {
                    Id = tt.Tag.Id,
                    Name = tt.Tag.Name,
                    Color = tt.Tag.Color
                }).ToList()
            })
            .ToListAsync();

        return transactions;
    }

    // ============================================
    // Private Helpers
    // ============================================

    private static FinancialGoalDto MapToDto(FinancialGoal goal)
    {
        return new FinancialGoalDto
        {
            Id = goal.Id,
            Title = goal.Title,
            TargetAmount = goal.TargetAmount,
            CurrencyCode = goal.CurrencyCode,
            CurrentAmount = goal.CurrentAmount,
            Deadline = goal.Deadline,
            Description = goal.Description,
            IsArchived = goal.IsArchived,
            CreatedAt = goal.CreatedAt,
            UpdatedAt = goal.UpdatedAt,
            CompletedAt = goal.CompletedAt,
            IsOverdue = goal.IsOverdue(),
            IsDeadlineApproaching = goal.IsDeadlineApproaching()
        };
    }
}
