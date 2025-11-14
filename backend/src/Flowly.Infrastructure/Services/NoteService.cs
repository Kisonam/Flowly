using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class NoteService : INoteService
{
    private readonly AppDbContext _dbContext;
    private readonly IArchiveService _archiveService;

    public NoteService(AppDbContext dbContext, IArchiveService archiveService)
    {
        _dbContext = dbContext;
        _archiveService = archiveService;
    }

    public async Task<PagedResult<NoteDto>> GetAllAsync(Guid userId, NoteFilterDto filter)
    {
        var query = _dbContext.Notes
            .Include(n => n.NoteTags)
                .ThenInclude(nt => nt.Tag)
            .Where(n => n.UserId == userId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.ToLower();
            query = query.Where(n => n.Title.ToLower().Contains(searchTerm) || 
                                   n.Markdown.ToLower().Contains(searchTerm));
        }

        if (filter.TagIds != null && filter.TagIds.Any())
        {
            query = query.Where(n => n.NoteTags.Any(nt => filter.TagIds.Contains(nt.TagId)));
        }

        if (filter.IsArchived.HasValue)
        {
            query = query.Where(n => n.IsArchived == filter.IsArchived.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var notes = await query
            .OrderByDescending(n => n.UpdatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        // Map to DTOs
        var noteDtos = notes.Select(MapToNoteDto).ToList();

        return new PagedResult<NoteDto>
        {
            Items = noteDtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<NoteDto> GetByIdAsync(Guid userId, Guid noteId)
    {
        var note = await _dbContext.Notes
            .Include(n => n.NoteTags)
                .ThenInclude(nt => nt.Tag)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);

        if (note == null)
        {
            throw new InvalidOperationException("Note not found");
        }

        return MapToNoteDto(note);
    }

    public async Task<NoteDto> CreateAsync(Guid userId, CreateNoteDto dto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title is required", nameof(dto.Title));
        }

        if (string.IsNullOrWhiteSpace(dto.Markdown))
        {
            throw new ArgumentException("Content is required", nameof(dto.Markdown));
        }

        // Create note entity
        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title.Trim(),
            Markdown = dto.Markdown,
            NoteGroupId = dto.GroupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Notes.Add(note);

        // Add tags if provided
        if (dto.TagIds != null && dto.TagIds.Any())
        {
            await AddTagsToNoteAsync(note.Id, userId, dto.TagIds);
        }

        await _dbContext.SaveChangesAsync();

        // Reload with tags
        return await GetByIdAsync(userId, note.Id);
    }

    public async Task<NoteDto> UpdateAsync(Guid userId, Guid noteId, UpdateNoteDto dto)
    {
        var note = await _dbContext.Notes
            .Include(n => n.NoteTags)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);

        if (note == null)
        {
            throw new InvalidOperationException("Note not found");
        }

        // Update fields if provided
        var hasChanges = false;

        if (!string.IsNullOrWhiteSpace(dto.Title) && dto.Title != note.Title)
        {
            note.Title = dto.Title.Trim();
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Markdown) && dto.Markdown != note.Markdown)
        {
            note.Markdown = dto.Markdown;
            note.HtmlCache = null; // Invalidate cache
            hasChanges = true;
        }

        if (hasChanges)
        {
            note.UpdatedAt = DateTime.UtcNow;
        }

        // Update group if provided
        if (dto.GroupId != null)
        {
            note.NoteGroupId = dto.GroupId;
            note.UpdatedAt = DateTime.UtcNow;
        }

        // Update tags if provided
        if (dto.TagIds != null)
        {
            // Remove existing tags
            var existingTags = note.NoteTags.ToList();
            _dbContext.NoteTags.RemoveRange(existingTags);

            // Add new tags
            if (dto.TagIds.Any())
            {
                await AddTagsToNoteAsync(noteId, userId, dto.TagIds);
            }
        }

        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(userId, noteId);
    }

    public async Task ArchiveAsync(Guid userId, Guid noteId)
    {
        await _archiveService.ArchiveEntityAsync(userId, LinkEntityType.Note, noteId);
    }

    public async Task RestoreAsync(Guid userId, Guid noteId)
    {
        // For direct restore by entity ID, we need to find the archive entry
        // This is a simplified version - in production you might want to handle this differently
        var note = await _dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);

        if (note == null)
        {
            throw new InvalidOperationException("Note not found");
        }

        note.Restore();
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddTagAsync(Guid userId, Guid noteId, Guid tagId)
    {
        // Verify note belongs to user
        var note = await _dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);

        if (note == null)
        {
            throw new InvalidOperationException("Note not found");
        }

        // Verify tag belongs to user
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

        if (tag == null)
        {
            throw new InvalidOperationException("Tag not found");
        }

        // Check if tag already added
        var existingNoteTag = await _dbContext.NoteTags
            .FirstOrDefaultAsync(nt => nt.NoteId == noteId && nt.TagId == tagId);

        if (existingNoteTag != null)
        {
            return; // Already exists, no need to add
        }

        // Add tag
        var noteTag = new NoteTag
        {
            NoteId = noteId,
            TagId = tagId
        };

        _dbContext.NoteTags.Add(noteTag);
        
        // Update note timestamp
        note.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveTagAsync(Guid userId, Guid noteId, Guid tagId)
    {
        // Verify note belongs to user
        var note = await _dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);

        if (note == null)
        {
            throw new InvalidOperationException("Note not found");
        }

        // Find and remove note-tag relation
        var noteTag = await _dbContext.NoteTags
            .FirstOrDefaultAsync(nt => nt.NoteId == noteId && nt.TagId == tagId);

        if (noteTag != null)
        {
            _dbContext.NoteTags.Remove(noteTag);
            
            // Update note timestamp
            note.UpdatedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<string> UploadMediaAsync(Guid userId, Guid noteId, Stream fileStream, string fileName, string contentType)
    {
        // Verify note belongs to user
        var note = await _dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);

        if (note == null)
        {
            throw new InvalidOperationException("Note not found");
        }

        // Validate file type
        var allowedTypes = new[] 
        { 
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
            "application/pdf",
            "video/mp4", "video/webm"
        };
        
        if (!allowedTypes.Contains(contentType.ToLower()))
        {
            throw new InvalidOperationException("Invalid file type. Only images, PDFs, and videos are allowed");
        }

        // Validate file size (max 50MB)
        if (fileStream.Length > 50 * 1024 * 1024)
        {
            throw new InvalidOperationException("File size exceeds 50MB limit");
        }

        // Generate unique filename
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var uploadPath = Path.Combine("uploads", "notes", userId.ToString(), noteId.ToString());
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", uploadPath);

        // Create directory if not exists
        Directory.CreateDirectory(fullPath);

        // Save file
        var filePath = Path.Combine(fullPath, uniqueFileName);
        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        // Create media asset entity
        var fileUrl = $"/uploads/notes/{userId}/{noteId}/{uniqueFileName}";
        var mediaAsset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = fileName,
            Path = fileUrl,
            Size = fileStream.Length,
            MimeType = contentType,
            NoteId = noteId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MediaAssets.Add(mediaAsset);
        
        // Update note timestamp
        note.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();

        return fileUrl;
    }

    public async Task<string> ExportMarkdownAsync(Guid userId, Guid noteId)
    {
        var note = await _dbContext.Notes
            .Include(n => n.NoteTags)
                .ThenInclude(nt => nt.Tag)
            .FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == userId);

        if (note == null)
        {
            throw new InvalidOperationException("Note not found");
        }

        // Build markdown content with metadata
        var markdown = $"# {note.Title}\n\n";
        
        if (note.NoteTags.Any())
        {
            var tags = string.Join(", ", note.NoteTags.Select(nt => $"#{nt.Tag.Name}"));
            markdown += $"**Tags:** {tags}\n\n";
        }
        
        markdown += $"**Created:** {note.CreatedAt:yyyy-MM-dd HH:mm:ss}\n";
        markdown += $"**Updated:** {note.UpdatedAt:yyyy-MM-dd HH:mm:ss}\n\n";
        markdown += "---\n\n";
        markdown += note.Markdown;

        return markdown;
    }

    // Private helper methods
    private async Task AddTagsToNoteAsync(Guid noteId, Guid userId, List<Guid> tagIds)
    {
        // Verify all tags belong to user
        var tags = await _dbContext.Tags
            .Where(t => tagIds.Contains(t.Id) && t.UserId == userId)
            .ToListAsync();

        if (tags.Count != tagIds.Count)
        {
            throw new InvalidOperationException("One or more tags not found");
        }

        // Add note-tag relations
        var noteTags = tagIds.Select(tagId => new NoteTag
        {
            NoteId = noteId,
            TagId = tagId
        }).ToList();

        _dbContext.NoteTags.AddRange(noteTags);
    }

    private NoteDto MapToNoteDto(Note note)
    {
        return new NoteDto
        {
            Id = note.Id,
            Title = note.Title,
            Markdown = note.Markdown,
            HtmlCache = note.HtmlCache,
            IsArchived = note.IsArchived,
            GroupId = note.NoteGroupId,
            Tags = note.NoteTags.Select(nt => new TagDto
            {
                Id = nt.Tag.Id,
                Name = nt.Tag.Name,
                Color = nt.Tag.Color
            }).ToList(),
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}
