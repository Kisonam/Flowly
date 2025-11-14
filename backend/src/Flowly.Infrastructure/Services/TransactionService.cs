using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly IArchiveService _archiveService;

    public TransactionService(AppDbContext dbContext, IArchiveService archiveService)
    {
        _dbContext = dbContext;
        _archiveService = archiveService;
    }

    // ============================================
    // CRUD & Query
    // ============================================

    public async Task<PagedResult<TransactionListItemDto>> GetAllAsync(Guid userId, TransactionFilterDto filter)
    {
        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(t => 
                t.Title.ToLower().Contains(search) || 
                (t.Description != null && t.Description.ToLower().Contains(search)));
        }

        if (filter.DateFrom.HasValue)
        {
            var dateFrom = DateTime.SpecifyKind(filter.DateFrom.Value, DateTimeKind.Utc);
            query = query.Where(t => t.Date >= dateFrom);
        }

        if (filter.DateTo.HasValue)
        {
            var dateTo = DateTime.SpecifyKind(filter.DateTo.Value, DateTimeKind.Utc);
            query = query.Where(t => t.Date <= dateTo);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CurrencyCode))
        {
            query = query.Where(t => t.CurrencyCode == filter.CurrencyCode);
        }

        if (filter.IsArchived.HasValue)
        {
            query = query.Where(t => t.IsArchived == filter.IsArchived.Value);
        }

        // Tag filtering via TransactionTags
        if (filter.TagIds != null && filter.TagIds.Any())
        {
            query = query.Where(t => t.TransactionTags.Any(tt => filter.TagIds.Contains(tt.TagId)));
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and include related entities
        var transactions = await query
            .Include(t => t.Category)
            .Include(t => t.Budget)
            .Include(t => t.Goal)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TransactionListItemDto
            {
                Id = t.Id,
                Title = t.Title,
                Amount = t.Amount,
                CurrencyCode = t.CurrencyCode,
                Type = t.Type,
                CategoryId = t.CategoryId,
                BudgetId = t.BudgetId,
                GoalId = t.GoalId,
                Date = t.Date,
                CreatedAt = t.CreatedAt,
                Description = t.Description,
                IsArchived = t.IsArchived,
                Category = t.Category != null ? new CategoryDto
                {
                    Id = t.Category.Id,
                    Name = t.Category.Name,
                    UserId = t.Category.UserId
                } : null,
                Budget = t.Budget != null ? new BudgetSummaryDto
                {
                    Id = t.Budget.Id,
                    Title = t.Budget.Title,
                    CurrencyCode = t.Budget.CurrencyCode
                } : null,
                Goal = t.Goal != null ? new GoalSummaryDto
                {
                    Id = t.Goal.Id,
                    Title = t.Goal.Title,
                    CurrencyCode = t.Goal.CurrencyCode
                } : null,
                Tags = t.TransactionTags.Select(tt => new TagDto
                {
                    Id = tt.Tag.Id,
                    Name = tt.Tag.Name,
                    Color = tt.Tag.Color
                }).ToList()
            })
            .ToListAsync();

        return new PagedResult<TransactionListItemDto>
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<TransactionDto> GetByIdAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Budget)
            .Include(t => t.Goal)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        var tags = transaction.TransactionTags
            .Select(tt => new TagDto
            {
                Id = tt.Tag.Id,
                Name = tt.Tag.Name,
                Color = tt.Tag.Color
            })
            .ToList();

        return MapToDto(transaction, tags);
    }

    public async Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionDto dto)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(dto.Title));
        }
        
        if (dto.Amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(dto.Amount));
        }

        // Verify category exists and belongs to user (if provided)
        if (dto.CategoryId.HasValue)
        {
            var category = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value 
                    && (c.UserId == userId || c.UserId == null));

            if (category == null)
            {
                throw new InvalidOperationException("Category not found");
            }
        }

        // Verify currency exists
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
        }

        // Verify budget exists if provided
        if (dto.BudgetId.HasValue)
        {
            var budget = await _dbContext.Budgets
                .FirstOrDefaultAsync(b => b.Id == dto.BudgetId.Value && b.UserId == userId);

            if (budget == null)
            {
                throw new InvalidOperationException("Budget not found");
            }

            // Validate currency match - budget and transaction must use same currency
            if (budget.CurrencyCode != dto.CurrencyCode)
            {
                throw new InvalidOperationException(
                    $"Transaction currency ({dto.CurrencyCode}) must match budget currency ({budget.CurrencyCode}). " +
                    "Currency conversion is not supported.");
            }
        }

        // Verify goal exists if provided
        if (dto.GoalId.HasValue)
        {
            var goal = await _dbContext.FinancialGoals
                .FirstOrDefaultAsync(g => g.Id == dto.GoalId.Value && g.UserId == userId);

            if (goal == null)
            {
                throw new InvalidOperationException("Financial goal not found");
            }

            // Validate currency match - goal and transaction must use same currency
            if (goal.CurrencyCode != dto.CurrencyCode)
            {
                throw new InvalidOperationException(
                    $"Transaction currency ({dto.CurrencyCode}) must match goal currency ({goal.CurrencyCode}). " +
                    "Currency conversion is not supported.");
            }

            // Check if goal is already completed (cannot add transactions to completed goals)
            if (goal.CurrentAmount >= goal.TargetAmount && dto.Type == TransactionType.Income)
            {
                throw new InvalidOperationException(
                    $"Goal '{goal.Title}' is already completed. " +
                    $"Current: {goal.CurrentAmount:F2} {goal.CurrencyCode}, " +
                    $"Target: {goal.TargetAmount:F2} {goal.CurrencyCode}. " +
                    "You cannot add more income to a completed goal.");
            }

            // Check if there are sufficient funds for expense transactions
            if (dto.Type == TransactionType.Expense && goal.CurrentAmount < dto.Amount)
            {
                throw new InvalidOperationException(
                    $"Insufficient funds in goal '{goal.Title}'. " +
                    $"Available: {goal.CurrentAmount:F2} {goal.CurrencyCode}, " +
                    $"Required: {dto.Amount:F2} {dto.CurrencyCode}");
            }
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title.Trim(),
            Amount = dto.Amount,
            CurrencyCode = dto.CurrencyCode,
            Type = dto.Type,
            CategoryId = dto.CategoryId,
            BudgetId = dto.BudgetId,
            GoalId = dto.GoalId,
            Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            Description = dto.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);

        // Add tags if provided
        if (dto.TagIds != null && dto.TagIds.Any())
        {
            await AddTagsToTransactionAsync(transaction.Id, userId, dto.TagIds);
        }

        await _dbContext.SaveChangesAsync();

        // Update goal currentAmount if transaction is linked to a goal
        if (dto.GoalId.HasValue)
        {
            await UpdateGoalAmountAsync(userId, dto.GoalId.Value);
        }

        return await GetByIdAsync(userId, transaction.Id);
    }

    public async Task<TransactionDto> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionDto dto)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.TransactionTags)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        // Store old goal ID to update its amount later
        var oldGoalId = transaction.GoalId;

        // Verify category exists (if provided)
        if (dto.CategoryId.HasValue)
        {
            var category = await _dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId.Value 
                    && (c.UserId == userId || c.UserId == null));

            if (category == null)
            {
                throw new InvalidOperationException("Category not found");
            }
        }

        // Verify currency exists
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
        }

        // Verify budget exists if provided
        if (dto.BudgetId.HasValue)
        {
            var budget = await _dbContext.Budgets
                .FirstOrDefaultAsync(b => b.Id == dto.BudgetId.Value && b.UserId == userId);

            if (budget == null)
            {
                throw new InvalidOperationException("Budget not found");
            }

            // Validate currency match - budget and transaction must use same currency
            if (budget.CurrencyCode != dto.CurrencyCode)
            {
                throw new InvalidOperationException(
                    $"Transaction currency ({dto.CurrencyCode}) must match budget currency ({budget.CurrencyCode}). " +
                    "Currency conversion is not supported.");
            }
        }

        // Verify goal exists if provided
        if (dto.GoalId.HasValue)
        {
            var goal = await _dbContext.FinancialGoals
                .FirstOrDefaultAsync(g => g.Id == dto.GoalId.Value && g.UserId == userId);

            if (goal == null)
            {
                throw new InvalidOperationException("Financial goal not found");
            }

            // Validate currency match - goal and transaction must use same currency
            if (goal.CurrencyCode != dto.CurrencyCode)
            {
                throw new InvalidOperationException(
                    $"Transaction currency ({dto.CurrencyCode}) must match goal currency ({goal.CurrencyCode}). " +
                    "Currency conversion is not supported.");
            }

            // Calculate current amount excluding the old transaction if it was linked to this goal
            var currentAmount = goal.CurrentAmount;
            
            // If this transaction was already linked to this goal, subtract its effect first
            if (oldGoalId == dto.GoalId)
            {
                // Reverse the old transaction's effect
                currentAmount -= (transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount);
            }

            // Check if goal would be over-filled by adding income
            if (dto.Type == TransactionType.Income)
            {
                var futureAmount = currentAmount + dto.Amount;
                if (futureAmount > goal.TargetAmount && goal.TargetAmount > 0)
                {
                    var remaining = Math.Max(0, goal.TargetAmount - currentAmount);
                    throw new InvalidOperationException(
                        $"This income would exceed the goal target. " +
                        $"Current: {currentAmount:F2} {goal.CurrencyCode}, " +
                        $"Target: {goal.TargetAmount:F2} {goal.CurrencyCode}, " +
                        $"Maximum allowed: {remaining:F2} {goal.CurrencyCode}");
                }
            }

            // Check if there are sufficient funds for expense transactions
            if (dto.Type == TransactionType.Expense)
            {
                // Check if there are sufficient funds after applying the new expense
                if (currentAmount < dto.Amount)
                {
                    throw new InvalidOperationException(
                        $"Insufficient funds in goal '{goal.Title}'. " +
                        $"Available: {currentAmount:F2} {goal.CurrencyCode}, " +
                        $"Required: {dto.Amount:F2} {dto.CurrencyCode}");
                }
            }
        }

        transaction.Update(
            dto.Title.Trim(),
            dto.Amount,
            dto.CurrencyCode,
            dto.Type,
            dto.CategoryId,
            DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            dto.Description?.Trim(),
            dto.BudgetId,
            dto.GoalId
        );

        // Update tags if provided
        if (dto.TagIds != null)
        {
            // Remove existing tag links
            var existingTags = transaction.TransactionTags.ToList();
            _dbContext.TransactionTags.RemoveRange(existingTags);

            // Add new tags
            if (dto.TagIds.Any())
            {
                await AddTagsToTransactionAsync(transactionId, userId, dto.TagIds);
            }
        }

        await _dbContext.SaveChangesAsync();

        // Update goal amounts if goal was changed or modified
        if (oldGoalId.HasValue && oldGoalId != dto.GoalId)
        {
            // Update old goal (transaction was removed from it)
            await UpdateGoalAmountAsync(userId, oldGoalId.Value);
        }
        
        if (dto.GoalId.HasValue)
        {
            // Update new goal (transaction was added to it or its amount changed)
            await UpdateGoalAmountAsync(userId, dto.GoalId.Value);
        }

        return await GetByIdAsync(userId, transactionId);
    }

    public async Task ArchiveAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        var goalId = transaction.GoalId;
        
        await _archiveService.ArchiveEntityAsync(userId, LinkEntityType.Transaction, transactionId);

        // Update goal amount after archiving (archived transactions don't count)
        if (goalId.HasValue)
        {
            await UpdateGoalAmountAsync(userId, goalId.Value);
        }
    }

    public async Task RestoreAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        var goalId = transaction.GoalId;
        
        transaction.Restore();
        await _dbContext.SaveChangesAsync();

        // Update goal amount after restoring
        if (goalId.HasValue)
        {
            await UpdateGoalAmountAsync(userId, goalId.Value);
        }
    }

    // ============================================
    // Stats
    // ============================================

    public async Task<FinanceStatsDto> GetStatsAsync(Guid userId, DateTime periodStart, DateTime periodEnd, string? currencyCode = null)
    {
        periodStart = DateTime.SpecifyKind(periodStart.Date, DateTimeKind.Utc);
        periodEnd = DateTime.SpecifyKind(periodEnd.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId 
                && !t.IsArchived
                && t.Date >= periodStart 
                && t.Date <= periodEnd);

        // Filter by currency if provided
        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            query = query.Where(t => t.CurrencyCode == currencyCode);
        }

        var transactions = await query.ToListAsync();

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        // Stats by category
        var incomeByCategory = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => new { t.CategoryId, CategoryName = t.Category != null ? t.Category.Name : "Uncategorized" })
            .Select(g => new CategoryStatsDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Percentage = totalIncome > 0 ? (g.Sum(t => t.Amount) / totalIncome * 100) : 0
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var expenseByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.CategoryId, CategoryName = t.Category != null ? t.Category.Name : "Uncategorized" })
            .Select(g => new CategoryStatsDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Percentage = totalExpense > 0 ? (g.Sum(t => t.Amount) / totalExpense * 100) : 0
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        // Stats by month
        var byMonth = transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthStatsDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                TotalIncome = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                TotalExpense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                NetAmount = g.Sum(t => t.GetSignedAmount()),
                TransactionCount = g.Count()
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        var dayCount = (periodEnd.Date - periodStart.Date).Days + 1;

        return new FinanceStatsDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetAmount = totalIncome - totalExpense,
            CurrencyCode = currencyCode ?? "Mixed",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            IncomeByCategory = incomeByCategory,
            ExpenseByCategory = expenseByCategory,
            ByMonth = byMonth,
            AverageDailyIncome = dayCount > 0 ? totalIncome / dayCount : 0,
            AverageDailyExpense = dayCount > 0 ? totalExpense / dayCount : 0,
            TotalTransactionCount = transactions.Count
        };
    }

    // ============================================
    // Private Helpers
    // ============================================

    private async Task AddTagsToTransactionAsync(Guid transactionId, Guid userId, List<Guid> tagIds)
    {
        // Verify all tags belong to user
        var tags = await _dbContext.Tags
            .Where(t => tagIds.Contains(t.Id) && t.UserId == userId)
            .ToListAsync();

        if (tags.Count != tagIds.Count)
        {
            throw new InvalidOperationException("One or more tags not found");
        }

        // Create transaction-tag relations
        var transactionTags = tagIds.Select(tagId => new TransactionTag
        {
            TransactionId = transactionId,
            TagId = tagId
        }).ToList();

        _dbContext.TransactionTags.AddRange(transactionTags);
    }

    private static TransactionDto MapToDto(Transaction transaction, List<TagDto> tags)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            Title = transaction.Title,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            Type = transaction.Type,
            CategoryId = transaction.CategoryId,
            BudgetId = transaction.BudgetId,
            GoalId = transaction.GoalId,
            Date = transaction.Date,
            Description = transaction.Description,
            IsArchived = transaction.IsArchived,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
            Category = transaction.Category != null ? new CategoryDto
            {
                Id = transaction.Category.Id,
                Name = transaction.Category.Name,
                UserId = transaction.Category.UserId
            } : null,
            Budget = transaction.Budget != null ? new BudgetSummaryDto
            {
                Id = transaction.Budget.Id,
                Title = transaction.Budget.Title,
                CurrencyCode = transaction.Budget.CurrencyCode
            } : null,
            Goal = transaction.Goal != null ? new GoalSummaryDto
            {
                Id = transaction.Goal.Id,
                Title = transaction.Goal.Title,
                CurrencyCode = transaction.Goal.CurrencyCode
            } : null,
            Tags = tags
        };
    }

    /// <summary>
    /// Recalculate and update goal currentAmount based on all linked transactions
    /// </summary>
    private async Task UpdateGoalAmountAsync(Guid userId, Guid goalId)
    {
        var goal = await _dbContext.FinancialGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

        if (goal == null)
        {
            return; // Goal not found, skip update
        }

        // Calculate total from all transactions linked to this goal
        // Income adds (+), Expense subtracts (-)
        var totalAmount = await _dbContext.Transactions
            .Where(t => t.UserId == userId && t.GoalId == goalId && !t.IsArchived)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount);

        // Update goal's current amount
        // Note: This can be negative if expenses exceed income
        goal.SetCurrentAmount(totalAmount);
        await _dbContext.SaveChangesAsync();
    }
}
