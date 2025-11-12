using Flowly.Application.DTOs.Transactions;
using Flowly.Application.DTOs.Notes;
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
        Console.WriteLine($"🔍 BudgetService.GetAllAsync - UserId: {userId}, IsActive: {filter?.IsActive?.ToString() ?? "null"}");
        
        var query = _dbContext.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        // By default (when no filter provided), exclude archived budgets
        if (filter == null)
        {
            query = query.Where(b => !b.IsArchived);
        }

        // Apply filters
        if (filter != null)
        {
            // Filter by archived status only if explicitly specified
            if (filter.IsArchived.HasValue)
            {
                query = query.Where(b => b.IsArchived == filter.IsArchived.Value);
            }
            // If IsArchived is not specified, show all budgets (both archived and non-archived)

            if (filter.IsActive.HasValue)
            {
                var now = DateTime.UtcNow;
                Console.WriteLine($"🔍 Filtering by IsActive: {filter.IsActive.Value}, Now: {now}");
                // Only filter when IsActive is explicitly set to true
                // If IsActive is null, show all budgets (both active and inactive)
                if (filter.IsActive.Value)
                {
                    query = query.Where(b => b.PeriodStart <= now && b.PeriodEnd >= now);
                }
                // If IsActive is false, show only inactive (archived) budgets
                else
                {
                    query = query.Where(b => b.PeriodStart > now || b.PeriodEnd < now);
                }
            }
            else
            {
                Console.WriteLine($"🔍 IsActive is null - showing ALL budgets");
            }
            // If IsActive is null/undefined, don't apply any active/inactive filter

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
                Console.WriteLine($"🔍 Filtering by DateFrom: {dateFrom:yyyy-MM-dd} - showing budgets starting on or after this date");
                query = query.Where(b => b.PeriodStart >= dateFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dateTo = DateTime.SpecifyKind(filter.DateTo.Value, DateTimeKind.Utc);
                Console.WriteLine($"🔍 Filtering by DateTo: {dateTo:yyyy-MM-dd} - showing budgets ending on or before this date");
                query = query.Where(b => b.PeriodEnd <= dateTo);
            }
        }

        var budgets = await query
            .OrderByDescending(b => b.PeriodStart)
            .ToListAsync();

        Console.WriteLine($"📊 Found {budgets.Count} budgets in database");
        foreach (var b in budgets)
        {
            var now = DateTime.UtcNow;
            var isActive = b.PeriodStart <= now && b.PeriodEnd >= now;
            Console.WriteLine($"  - Budget: {b.Title}, Period: {b.PeriodStart:yyyy-MM-dd} to {b.PeriodEnd:yyyy-MM-dd}, IsActive: {isActive}");
        }

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
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

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
            Title = dto.Title,
            Description = dto.Description,
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
            dto.Title,
            dto.Description,
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

    public async Task ArchiveAsync(Guid userId, Guid budgetId)
    {
        var budget = await _dbContext.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        budget.Archive();
        await _dbContext.SaveChangesAsync();
    }

    public async Task RestoreAsync(Guid userId, Guid budgetId)
    {
        var budget = await _dbContext.Budgets
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        budget.Restore();
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

    public async Task<List<TransactionListItemDto>> GetBudgetTransactionsAsync(Guid userId, Guid budgetId)
    {
        // Verify budget exists and belongs to user
        var budget = await _dbContext.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

        if (budget == null)
        {
            throw new InvalidOperationException("Budget not found");
        }

        // Get all transactions linked to this budget
        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .Where(t => t.UserId == userId
                && t.BudgetId == budgetId
                && t.Date >= budget.PeriodStart
                && t.Date <= budget.PeriodEnd)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => new TransactionListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Amount = t.Amount,
                CurrencyCode = t.CurrencyCode,
                Date = t.Date,
                Type = t.Type,
                CategoryId = t.CategoryId,
                BudgetId = t.BudgetId,
                CreatedAt = t.CreatedAt,
                Description = t.Description,
                IsArchived = t.IsArchived,
                Category = t.Category != null ? new CategoryDto
                {
                    Id = t.Category.Id,
                    Name = t.Category.Name,
                    UserId = t.Category.UserId
                } : null,
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

    private async Task<decimal> CalculateCurrentSpentAsync(Guid userId, Budget budget)
    {
        // Get all transactions linked to this budget
        var linkedTransactions = await _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && !t.IsArchived
                && t.BudgetId == budget.Id
                && t.CurrencyCode == budget.CurrencyCode
                && t.Date >= budget.PeriodStart
                && t.Date <= budget.PeriodEnd)
            .ToListAsync();

        // Calculate net spent:
        // - Expense transactions subtract from budget (add to spent) - positive
        // - Income transactions add to budget (reduce spent) - negative
        var spent = linkedTransactions
            .Sum(t => t.Type == TransactionType.Expense ? t.Amount : -t.Amount);

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
            Title = budget.Title,
            Description = budget.Description,
            PeriodStart = budget.PeriodStart,
            PeriodEnd = budget.PeriodEnd,
            Limit = budget.Limit,
            CurrencyCode = budget.CurrencyCode,
            CategoryId = budget.CategoryId,
            CurrentSpent = currentSpent,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt,
            IsArchived = budget.IsArchived,
            ArchivedAt = budget.ArchivedAt,
            Category = budget.Category != null ? new CategoryDto
            {
                Id = budget.Category.Id,
                Name = budget.Category.Name,
                Color = budget.Category.Color,
                Icon = budget.Category.Icon,
                UserId = budget.Category.UserId
            } : null,
            IsActive = isActive,
            DaysRemaining = daysRemaining
        };
    }
}
