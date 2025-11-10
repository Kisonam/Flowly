using Flowly.Application.DTOs.Transactions;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.Interfaces;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class TransactionQueryService : ITransactionQueryService
{
    private readonly AppDbContext _dbContext;

    public TransactionQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TransactionListItemDto>> GetListAsync(Guid userId, string? search = null, bool? isArchived = null, int take = 50)
    {
        if (take < 1) take = 1;
        if (take > 100) take = 100;

        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(t =>
                (t.Description != null && t.Description.ToLower().Contains(term)) ||
                t.Amount.ToString().Contains(term));
        }

        if (isArchived.HasValue)
        {
            query = query.Where(t => t.IsArchived == isArchived.Value);
        }

        return await query
            .OrderByDescending(t => t.Date)
            .Take(take)
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
    }
}
