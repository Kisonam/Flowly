using Flowly.Application.DTOs.Links;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class LinkService : ILinkService
{
    private readonly AppDbContext _dbContext;

    public LinkService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LinkDto> CreateLinkAsync(Guid userId, CreateLinkDto dto)
    {
        
        await ValidateEntityOwnershipAsync(userId, dto.FromType, dto.FromId);
        await ValidateEntityOwnershipAsync(userId, dto.ToType, dto.ToId);

        var existingLink = await _dbContext.Links
            .FirstOrDefaultAsync(l => 
                (l.FromType == dto.FromType && l.FromId == dto.FromId && l.ToType == dto.ToType && l.ToId == dto.ToId) ||
                (l.FromType == dto.ToType && l.FromId == dto.ToId && l.ToType == dto.FromType && l.ToId == dto.FromId));

        if (existingLink != null)
        {
            throw new InvalidOperationException("Link already exists between these entities");
        }

        var link = new Link
        {
            Id = Guid.NewGuid(),
            FromType = dto.FromType,
            FromId = dto.FromId,
            ToType = dto.ToType,
            ToId = dto.ToId,
            CreatedAt = DateTime.UtcNow
        };

        if (!link.IsValid())
        {
            throw new InvalidOperationException("Cannot create a link from an entity to itself");
        }

        _dbContext.Links.Add(link);
        await _dbContext.SaveChangesAsync();

        return await MapToLinkDtoAsync(userId, link);
    }

    public async Task DeleteLinkAsync(Guid userId, Guid linkId)
    {
        var link = await _dbContext.Links.FindAsync(linkId);
        
        if (link == null)
        {
            throw new KeyNotFoundException($"Link with ID {linkId} not found");
        }

        await ValidateEntityOwnershipAsync(userId, link.FromType, link.FromId);
        await ValidateEntityOwnershipAsync(userId, link.ToType, link.ToId);

        _dbContext.Links.Remove(link);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<LinkDto>> GetLinksForEntityAsync(Guid userId, LinkEntityType entityType, Guid entityId)
    {
        
        await ValidateEntityOwnershipAsync(userId, entityType, entityId);

        var links = await _dbContext.Links
            .Where(l => (l.FromType == entityType && l.FromId == entityId) ||
                       (l.ToType == entityType && l.ToId == entityId))
            .ToListAsync();

        var linkDtos = new List<LinkDto>();
        foreach (var link in links)
        {
            linkDtos.Add(await MapToLinkDtoAsync(userId, link));
        }

        return linkDtos;
    }

    public async Task<EntityPreviewDto> GetPreviewAsync(Guid userId, LinkEntityType entityType, Guid entityId)
    {
        
        await ValidateEntityOwnershipAsync(userId, entityType, entityId);

        return await GeneratePreviewAsync(entityType, entityId);
    }

    private async Task ValidateEntityOwnershipAsync(Guid userId, LinkEntityType entityType, Guid entityId)
    {
        bool exists = entityType switch
        {
            LinkEntityType.Note => await _dbContext.Notes
                .AnyAsync(n => n.Id == entityId && n.UserId == userId),
            
            LinkEntityType.Task => await _dbContext.Tasks
                .AnyAsync(t => t.Id == entityId && t.UserId == userId),
            
            LinkEntityType.Transaction => await _dbContext.Transactions
                .AnyAsync(tr => tr.Id == entityId && tr.UserId == userId),
            
            _ => throw new ArgumentException($"Unknown entity type: {entityType}")
        };

        if (!exists)
        {
            throw new KeyNotFoundException($"{entityType} with ID {entityId} not found or does not belong to user");
        }
    }

    private async Task<EntityPreviewDto> GeneratePreviewAsync(LinkEntityType entityType, Guid entityId)
    {
        return entityType switch
        {
            LinkEntityType.Note => await GenerateNotePreviewAsync(entityId),
            LinkEntityType.Task => await GenerateTaskPreviewAsync(entityId),
            LinkEntityType.Transaction => await GenerateTransactionPreviewAsync(entityId),
            _ => throw new ArgumentException($"Unknown entity type: {entityType}")
        };
    }

    private async Task<EntityPreviewDto> GenerateNotePreviewAsync(Guid noteId)
    {
        var note = await _dbContext.Notes
            .Where(n => n.Id == noteId)
            .Select(n => new { n.Id, n.Title, n.Markdown })
            .FirstOrDefaultAsync();

        if (note == null)
        {
            throw new KeyNotFoundException($"Note with ID {noteId} not found");
        }

        var snippet = note.Markdown.Length > 150 
            ? note.Markdown.Substring(0, 150) + "..." 
            : note.Markdown;

        return new EntityPreviewDto
        {
            Type = LinkEntityType.Note,
            Id = note.Id,
            Title = note.Title,
            Snippet = snippet,
            IconUrl = null 
        };
    }

    private async Task<EntityPreviewDto> GenerateTaskPreviewAsync(Guid taskId)
    {
        var task = await _dbContext.Tasks
            .Where(t => t.Id == taskId)
            .Select(t => new { t.Id, t.Title, t.Description, t.Status, t.DueDate })
            .FirstOrDefaultAsync();

        if (task == null)
        {
            throw new KeyNotFoundException($"Task with ID {taskId} not found");
        }

        var snippetParts = new List<string>();
        
        if (!string.IsNullOrEmpty(task.Description))
        {
            var desc = task.Description.Length > 100 
                ? task.Description.Substring(0, 100) + "..." 
                : task.Description;
            snippetParts.Add(desc);
        }
        
        snippetParts.Add($"Status: {task.Status}");
        
        if (task.DueDate.HasValue)
        {
            snippetParts.Add($"Due: {task.DueDate.Value:yyyy-MM-dd}");
        }

        return new EntityPreviewDto
        {
            Type = LinkEntityType.Task,
            Id = task.Id,
            Title = task.Title,
            Snippet = string.Join(" | ", snippetParts),
            IconUrl = null
        };
    }

    private async Task<EntityPreviewDto> GenerateTransactionPreviewAsync(Guid transactionId)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.Category)
            .Where(t => t.Id == transactionId)
            .Select(t => new 
            { 
                t.Id, 
                t.Title, 
                t.Amount, 
                t.CurrencyCode, 
                t.Type, 
                t.Date,
                CategoryName = t.Category != null ? t.Category.Name : null
            })
            .FirstOrDefaultAsync();

        if (transaction == null)
        {
            throw new KeyNotFoundException($"Transaction with ID {transactionId} not found");
        }

        var snippetParts = new List<string>
        {
            $"{transaction.Amount:F2} {transaction.CurrencyCode}",
            transaction.Type.ToString(),
            $"Date: {transaction.Date:yyyy-MM-dd}"
        };

        if (!string.IsNullOrEmpty(transaction.CategoryName))
        {
            snippetParts.Add($"Category: {transaction.CategoryName}");
        }

        return new EntityPreviewDto
        {
            Type = LinkEntityType.Transaction,
            Id = transaction.Id,
            Title = transaction.Title,
            Snippet = string.Join(" | ", snippetParts),
            IconUrl = null
        };
    }

    private async Task<LinkDto> MapToLinkDtoAsync(Guid userId, Link link)
    {
        var fromPreview = await GeneratePreviewAsync(link.FromType, link.FromId);
        var toPreview = await GeneratePreviewAsync(link.ToType, link.ToId);

        return new LinkDto
        {
            Id = link.Id,
            FromType = link.FromType,
            FromId = link.FromId,
            ToType = link.ToType,
            ToId = link.ToId,
            FromPreview = fromPreview,
            ToPreview = toPreview,
            CreatedAt = link.CreatedAt
        };
    }
}
