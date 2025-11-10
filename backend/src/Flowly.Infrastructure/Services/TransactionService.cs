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

    public TransactionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

        // Verify category exists and belongs to user
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId 
                && (c.UserId == userId || c.UserId == null));

        if (category == null)
        {
            throw new InvalidOperationException("Category not found");
        }

        // Verify currency exists
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
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

        // Verify category exists
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId 
                && (c.UserId == userId || c.UserId == null));

        if (category == null)
        {
            throw new InvalidOperationException("Category not found");
        }

        // Verify currency exists
        var currencyExists = await _dbContext.Currencies
            .AnyAsync(c => c.Code == dto.CurrencyCode);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency not found");
        }

        transaction.Update(
            dto.Title.Trim(),
            dto.Amount,
            dto.CurrencyCode,
            dto.Type,
            dto.CategoryId,
            DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            dto.Description?.Trim()
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

        transaction.Archive();
        await _dbContext.SaveChangesAsync();
    }

    public async Task RestoreAsync(Guid userId, Guid transactionId)
    {
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            throw new InvalidOperationException("Transaction not found");
        }

        transaction.Restore();
        await _dbContext.SaveChangesAsync();
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
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new CategoryStatsDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Percentage = totalIncome > 0 ? (g.Sum(t => t.Amount) / totalIncome * 100) : 0
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var expenseByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new CategoryStatsDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
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
            Tags = tags
        };
    }
}
