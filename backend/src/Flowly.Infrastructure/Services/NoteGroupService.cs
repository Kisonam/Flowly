using Flowly.Application.DTOs.Notes.Groups;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class NoteGroupService(AppDbContext db) : INoteGroupService
{
    private readonly AppDbContext _db = db;

    public async Task<List<NoteGroupDto>> GetAllAsync(Guid userId)
    {
        var groups = await _db.NoteGroups
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Order)
            .ToListAsync();

        return groups.Select(Map).ToList();
    }

    public async Task<NoteGroupDto> GetByIdAsync(Guid userId, Guid id)
    {
        var group = await _db.NoteGroups.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (group == null) throw new InvalidOperationException("Group not found");
        return Map(group);
    }

    public async Task<NoteGroupDto> CreateAsync(Guid userId, CreateNoteGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required", nameof(dto.Title));

        var maxOrder = await _db.NoteGroups.Where(g => g.UserId == userId).MaxAsync(g => (int?)g.Order) ?? -1;

        var group = new NoteGroup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = dto.Title.Trim(),
            Color = dto.Color,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.NoteGroups.Add(group);
        await _db.SaveChangesAsync();
        return Map(group);
    }

    public async Task<NoteGroupDto> UpdateAsync(Guid userId, Guid id, UpdateNoteGroupDto dto)
    {
        var group = await _db.NoteGroups.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (group == null) throw new InvalidOperationException("Group not found");

        if (!string.IsNullOrWhiteSpace(dto.Title)) group.UpdateTitle(dto.Title);
        if (dto.Color != null) group.UpdateColor(dto.Color);
        if (dto.Order.HasValue) group.UpdateOrder(dto.Order.Value);

        await _db.SaveChangesAsync();
        return Map(group);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var group = await _db.NoteGroups.Include(g => g.Notes)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (group == null) throw new InvalidOperationException("Group not found");

        // Unassign notes from this group
        foreach (var note in group.Notes)
        {
            note.NoteGroupId = null;
        }

        _db.NoteGroups.Remove(group);
        await _db.SaveChangesAsync();
    }

    private static NoteGroupDto Map(NoteGroup g) => new()
    {
        Id = g.Id,
        Title = g.Title,
        Order = g.Order,
        Color = g.Color
    };
}
