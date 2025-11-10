using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _dbContext;

    public BudgetService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<BudgetDto>> GetAllAsync(Guid userId, BudgetFilterDto? filter = null)
    {
        var query = _dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        // Apply filters
        if (filter != null)
        {
            if (filter.IsActive.HasValue)
            {
                var now = DateTime.UtcNow;
                if (filter.IsActive.Value)
                {
                    query = query.Where(b => b.PeriodStart <= now && b.PeriodEnd >= now);
                }
                else
                {
                    query = query.Where(b => b.PeriodStart > now || b.PeriodEnd < now);
                }
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == filter.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
            {
                query = query.Where(b => b.CurrencyCode == filter.CurrencyCode);
            }

            if (filter.DateFrom.HasValue)
            {
                var dateFrom = DateTime.SpecifyKind(filter.DateFrom.Value, DateTimeKind.Utc);
                query = query.Where(b => b.PeriodEnd >= dateFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dateTo = DateTime.SpecifyKind(filter.DateTo.Value, DateTimeKind.Utc);
                query = query.Where(b => b.PeriodStart <= dateTo);
            }
        }

        var budgets = await query
            .OrderByDescending(b => b.PeriodStart)
            .ToListAsync();

        // Calculate current spent for each budget
        var budgetDtos = new List<BudgetDto>();
        foreach (var budget in budgets)
        {
            var currentSpent = await CalculateCurrentSpentAsync(userId, budget);
            budgetDtos.Add(MapToDto(budget, currentSpent));
        }

        return budgetDtos;
    }

    public async Task<BudgetDto> GetByIdAsync(Guid userId, Guid budgetId)
    {
        var budget = await _dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        var currentSpent = await CalculateCurrentSpentAsync(userId, budget);
        return MapToDto(budget, currentSpent);
    }

    public async Task<BudgetDto> CreateAsync(Guid userId, CreateBudgetDto dto)
    {
        // Validate
        if (dto.PeriodEnd <= dto.PeriodStart)
        {
            throw new ArgumentException("Period end must be after period start");
        }

        if (dto.Limit <= 0)
        {
            throw new ArgumentException("Limit must be positive", nameof(dto.Limit));
        }

        // Verify category if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(c => c.Id == dto.CategoryId.Value 
                    && (c.UserId == userId || c.UserId == null));

            if (!categoryExists)
            {
                throw new InvalidOperationException("Category not found");
            }
        }

        // Verify currency
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
        }

        var budget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PeriodStart = DateTime.SpecifyKind(dto.PeriodStart, DateTimeKind.Utc),
            PeriodEnd = DateTime.SpecifyKind(dto.PeriodEnd, DateTimeKind.Utc),
            Limit = dto.Limit,
            CurrencyCode = dto.CurrencyCode,
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Budgets.Add(budget);
        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(userId, budget.Id);
    }

    public async Task<BudgetDto> UpdateAsync(Guid userId, Guid budgetId, UpdateBudgetDto dto)
    {
        var budget = await _dbContext.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        // Verify category if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(c => c.Id == dto.CategoryId.Value 
                    && (c.UserId == userId || c.UserId == null));

            if (!categoryExists)
            {
                throw new InvalidOperationException("Category not found");
            }
        }

        // Verify currency
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
        }

        budget.Update(
            DateTime.SpecifyKind(dto.PeriodStart, DateTimeKind.Utc),
            DateTime.SpecifyKind(dto.PeriodEnd, DateTimeKind.Utc),
            dto.Limit,
            dto.CurrencyCode,
            dto.CategoryId
        );

        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(userId, budgetId);
    }

    public async Task DeleteAsync(Guid userId, Guid budgetId)
    {
        var budget = await _dbContext.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        _dbContext.Budgets.Remove(budget);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> IsOverspentAsync(Guid userId, Guid budgetId)
    {
        var budget = await _dbContext.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        var currentSpent = await CalculateCurrentSpentAsync(userId, budget);
        return currentSpent > budget.Limit;
    }

    // ============================================
    // Private Helpers
    // ============================================

    private async Task<decimal> CalculateCurrentSpentAsync(Guid userId, Budget budget)
    {
        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && !t.IsArchived
                && t.Type == TransactionType.Expense
                && t.CurrencyCode == budget.CurrencyCode
                && t.Date >= budget.PeriodStart
                && t.Date <= budget.PeriodEnd);

        // Filter by category if budget is category-specific
        if (budget.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == budget.CategoryId.Value);
        }

        var spent = await query.SumAsync(t => (decimal?)t.Amount) ?? 0;
        return spent;
    }

    private static BudgetDto MapToDto(Budget budget, decimal currentSpent)
    {
        var now = DateTime.UtcNow;
        var isActive = budget.IsActive();
        var daysRemaining = budget.GetDaysRemaining();

        return new BudgetDto
        {
            Id = budget.Id,
            PeriodStart = budget.PeriodStart,
            PeriodEnd = budget.PeriodEnd,
            Limit = budget.Limit,
            CurrencyCode = budget.CurrencyCode,
            CategoryId = budget.CategoryId,
            CurrentSpent = currentSpent,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt,
            Category = budget.Category != null ? new CategoryDto
            {
                Id = budget.Category.Id,
                Name = budget.Category.Name,
                UserId = budget.Category.UserId
            } : null,
            IsActive = isActive,
            DaysRemaining = daysRemaining
        };
    }
}
