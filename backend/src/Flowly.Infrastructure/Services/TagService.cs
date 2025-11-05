using Flowly.Application.DTOs.Notes;
using Flowly.Application.DTOs.Tags;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _dbContext;

    public TagService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TagDto>> GetAllAsync(Guid userId)
    {
        var tags = await _dbContext.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color
            })
            .ToListAsync();

        return tags;
    }

    public async Task<TagDto> GetByIdAsync(Guid userId, Guid tagId)
    {
        var tag = await _dbContext.Tags
            .Where(t => t.UserId == userId && t.Id == tagId)
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color
            })
            .FirstOrDefaultAsync();

        if (tag == null)
        {
            throw new InvalidOperationException($"Tag with ID {tagId} not found");
        }

        return tag;
    }

    public async Task<TagDto> CreateAsync(Guid userId, CreateTagDto dto)
    {
        // Check if tag with same name already exists for this user
        var normalizedName = dto.Name.Trim().ToLowerInvariant();
        var existingTag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == normalizedName);

        if (existingTag != null)
        {
            throw new InvalidOperationException($"Tag with name '{dto.Name}' already exists");
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = normalizedName,
            Color = dto.Color,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tags.Add(tag);
        await _dbContext.SaveChangesAsync();

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color
        };
    }

    public async Task<TagDto> UpdateAsync(Guid userId, Guid tagId, UpdateTagDto dto)
    {
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == tagId);

        if (tag == null)
        {
            throw new InvalidOperationException($"Tag with ID {tagId} not found");
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            var normalizedName = dto.Name.Trim().ToLowerInvariant();
            
            // Check if another tag with same name exists
            var existingTag = await _dbContext.Tags
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == normalizedName && t.Id != tagId);

            if (existingTag != null)
            {
                throw new InvalidOperationException($"Tag with name '{dto.Name}' already exists");
            }

            tag.UpdateName(dto.Name);
        }

        if (dto.Color != null)
        {
            tag.UpdateColor(dto.Color);
        }

        await _dbContext.SaveChangesAsync();

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color
        };
    }

    public async Task DeleteAsync(Guid userId, Guid tagId)
    {
        var tag = await _dbContext.Tags
            .Include(t => t.NoteTags)
            .Include(t => t.TaskTags)
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == tagId);

        if (tag == null)
        {
            throw new InvalidOperationException($"Tag with ID {tagId} not found");
        }

        // Remove all associations
        _dbContext.NoteTags.RemoveRange(tag.NoteTags);
        _dbContext.TaskTags.RemoveRange(tag.TaskTags);
        _dbContext.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync();
    }
}
