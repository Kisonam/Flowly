using System.Text.Json;
using Flowly.Application.DTOs.Archive;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flowly.Infrastructure.Services;

/// <summary>
/// Service for managing archived entities with JSON snapshots
/// </summary>
public class ArchiveService : IArchiveService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ArchiveService> _logger;

    public ArchiveService(AppDbContext dbContext, ILogger<ArchiveService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ArchiveEntityAsync(Guid userId, LinkEntityType entityType, Guid entityId)
    {
        object? entity = null;
        string payloadJson;

        // Fetch entity and create JSON snapshot
        switch (entityType)
        {
            case LinkEntityType.Note:
                var note = await _dbContext.Notes
                    .Include(n => n.NoteTags)
                    .Include(n => n.MediaAssets)
                    .FirstOrDefaultAsync(n => n.Id == entityId && n.UserId == userId);

                if (note == null)
                    throw new InvalidOperationException("Note not found");

                note.Archive();
                payloadJson = SerializeEntity(note, entityType);
                entity = note;
                break;

            case LinkEntityType.Task:
                var task = await _dbContext.Tasks
                    .Include(t => t.Subtasks)
                    .Include(t => t.TaskTags)
                    .Include(t => t.Recurrence)
                    .FirstOrDefaultAsync(t => t.Id == entityId && t.UserId == userId);

                if (task == null)
                    throw new InvalidOperationException("Task not found");

                task.Archive();

                // Remove recurrence to avoid ghost scheduling
                if (task.Recurrence != null)
                {
                    _dbContext.TaskRecurrences.Remove(task.Recurrence);
                }

                payloadJson = SerializeEntity(task, entityType);
                entity = task;
                break;

            case LinkEntityType.Transaction:
                var transaction = await _dbContext.Transactions
                    .Include(t => t.TransactionTags)
                    .FirstOrDefaultAsync(t => t.Id == entityId && t.UserId == userId);

                if (transaction == null)
                    throw new InvalidOperationException("Transaction not found");

                transaction.Archive();
                payloadJson = SerializeEntity(transaction, entityType);
                entity = transaction;
                break;

            case LinkEntityType.Budget:
                var budget = await _dbContext.Budgets
                    .FirstOrDefaultAsync(b => b.Id == entityId && b.UserId == userId);

                if (budget == null)
                    throw new InvalidOperationException("Budget not found");

                budget.Archive();
                payloadJson = SerializeEntity(budget, entityType);
                entity = budget;
                break;

            case LinkEntityType.FinancialGoal:
                var goal = await _dbContext.FinancialGoals
                    .FirstOrDefaultAsync(g => g.Id == entityId && g.UserId == userId);

                if (goal == null)
                    throw new InvalidOperationException("Financial goal not found");

                goal.Archive();
                payloadJson = SerializeEntity(goal, entityType);
                entity = goal;
                break;

            default:
                throw new ArgumentException($"Unsupported entity type: {entityType}");
        }

        // Create archive entry
        var archiveEntry = new ArchiveEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            PayloadJson = payloadJson,
            ArchivedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Creating archive entry: EntityType={EntityType} (enum value: {EnumValue}), EntityId={EntityId}",
            archiveEntry.EntityType, (int)archiveEntry.EntityType, archiveEntry.EntityId);

        _dbContext.ArchiveEntries.Add(archiveEntry);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Archive entry saved to database");

        _logger.LogInformation("Archived {EntityType} {EntityId} for user {UserId}", entityType, entityId, userId);
    }

    /// <inheritdoc />
    public async Task RestoreEntityAsync(Guid userId, Guid archiveEntryId)
    {
        var archiveEntry = await _dbContext.ArchiveEntries
            .FirstOrDefaultAsync(a => a.Id == archiveEntryId && a.UserId == userId);

        if (archiveEntry == null)
            throw new InvalidOperationException("Archive entry not found");

        // Deserialize and restore entity
        switch (archiveEntry.EntityType)
        {
            case LinkEntityType.Note:
                var note = await _dbContext.Notes
                    .FirstOrDefaultAsync(n => n.Id == archiveEntry.EntityId && n.UserId == userId);

                if (note != null)
                {
                    note.Restore();
                }
                else
                {
                    // Entity was permanently deleted, recreate from snapshot
                    var restoredNote = DeserializeEntity<Note>(archiveEntry.PayloadJson);
                    restoredNote.Restore();
                    _dbContext.Notes.Add(restoredNote);
                }
                break;

            case LinkEntityType.Task:
                var task = await _dbContext.Tasks
                    .FirstOrDefaultAsync(t => t.Id == archiveEntry.EntityId && t.UserId == userId);

                if (task != null)
                {
                    task.Restore();
                }
                else
                {
                    var restoredTask = DeserializeEntity<TaskItem>(archiveEntry.PayloadJson);
                    restoredTask.Restore();
                    _dbContext.Tasks.Add(restoredTask);
                }
                break;

            case LinkEntityType.Transaction:
                var transaction = await _dbContext.Transactions
                    .FirstOrDefaultAsync(t => t.Id == archiveEntry.EntityId && t.UserId == userId);

                if (transaction != null)
                {
                    transaction.Restore();
                }
                else
                {
                    var restoredTransaction = DeserializeEntity<Transaction>(archiveEntry.PayloadJson);
                    restoredTransaction.Restore();
                    _dbContext.Transactions.Add(restoredTransaction);
                }
                break;

            case LinkEntityType.Budget:
                var budget = await _dbContext.Budgets
                    .FirstOrDefaultAsync(b => b.Id == archiveEntry.EntityId && b.UserId == userId);

                if (budget != null)
                {
                    budget.Restore();
                }
                else
                {
                    var restoredBudget = DeserializeEntity<Budget>(archiveEntry.PayloadJson);
                    restoredBudget.Restore();
                    _dbContext.Budgets.Add(restoredBudget);
                }
                break;

            case LinkEntityType.FinancialGoal:
                var goal = await _dbContext.FinancialGoals
                    .FirstOrDefaultAsync(g => g.Id == archiveEntry.EntityId && g.UserId == userId);

                if (goal != null)
                {
                    goal.Restore();
                }
                else
                {
                    var restoredGoal = DeserializeEntity<FinancialGoal>(archiveEntry.PayloadJson);
                    restoredGoal.Restore();
                    _dbContext.FinancialGoals.Add(restoredGoal);
                }
                break;

            default:
                throw new ArgumentException($"Unsupported entity type: {archiveEntry.EntityType}");
        }

        // Remove archive entry after restoration
        _dbContext.ArchiveEntries.Remove(archiveEntry);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Restored {EntityType} {EntityId} for user {UserId}",
            archiveEntry.EntityType, archiveEntry.EntityId, userId);
    }

    /// <inheritdoc />
    public async Task<ArchiveListResponseDto> GetArchivedAsync(Guid userId, ArchiveQueryDto query)
    {
        var archiveQuery = _dbContext.ArchiveEntries
            .Where(a => a.UserId == userId);

        // Filter by entity type
        if (query.EntityType.HasValue)
        {
            archiveQuery = archiveQuery.Where(a => a.EntityType == query.EntityType.Value);
        }

        // Search in JSON payload (basic text search)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            archiveQuery = archiveQuery.Where(a =>
                EF.Functions.Like(a.PayloadJson.ToLower(), $"%{searchLower}%"));
        }

        // Get total count
        var totalCount = await archiveQuery.CountAsync();

        // Sorting
        archiveQuery = query.SortBy.ToLower() switch
        {
            "title" => query.SortDirection.ToLower() == "asc"
                ? archiveQuery.OrderBy(a => a.PayloadJson)
                : archiveQuery.OrderByDescending(a => a.PayloadJson),
            "entitytype" => query.SortDirection.ToLower() == "asc"
                ? archiveQuery.OrderBy(a => a.EntityType)
                : archiveQuery.OrderByDescending(a => a.EntityType),
            _ => query.SortDirection.ToLower() == "asc"
                ? archiveQuery.OrderBy(a => a.ArchivedAt)
                : archiveQuery.OrderByDescending(a => a.ArchivedAt)
        };

        // Pagination
        var items = await archiveQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // Map to DTOs
        var dtos = items.Select(a => MapToDto(a)).ToList();

        return new ArchiveListResponseDto
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<ArchivedEntityDetailDto> GetArchivedDetailAsync(Guid userId, Guid archiveEntryId)
    {
        var archiveEntry = await _dbContext.ArchiveEntries
            .FirstOrDefaultAsync(a => a.Id == archiveEntryId && a.UserId == userId);

        if (archiveEntry == null)
            throw new InvalidOperationException("Archive entry not found");

        var detailDto = new ArchivedEntityDetailDto
        {
            Id = archiveEntry.Id,
            EntityType = archiveEntry.EntityType,
            EntityId = archiveEntry.EntityId,
            ArchivedAt = archiveEntry.ArchivedAt,
            PayloadJson = archiveEntry.PayloadJson
        };

        // Extract title and metadata using existing logic
        var baseDto = MapToDto(archiveEntry);
        detailDto.Title = baseDto.Title;
        detailDto.Description = baseDto.Description;
        detailDto.Metadata = baseDto.Metadata;

        return detailDto;
    }

    /// <inheritdoc />
    public async Task PermanentDeleteAsync(Guid userId, Guid archiveEntryId)
    {
        var archiveEntry = await _dbContext.ArchiveEntries
            .FirstOrDefaultAsync(a => a.Id == archiveEntryId && a.UserId == userId);

        if (archiveEntry == null)
            throw new InvalidOperationException("Archive entry not found");

        _dbContext.ArchiveEntries.Remove(archiveEntry);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Permanently deleted archive entry {ArchiveEntryId} for user {UserId}",
            archiveEntryId, userId);
    }

    // ============================================
    // Helper Methods
    // ============================================

    private string SerializeEntity(object entity, LinkEntityType entityType)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        return JsonSerializer.Serialize(entity, options);
    }

    private T DeserializeEntity<T>(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var entity = JsonSerializer.Deserialize<T>(json, options);
        if (entity == null)
            throw new InvalidOperationException("Failed to deserialize entity");

        return entity;
    }

    private ArchivedEntityDto MapToDto(ArchiveEntry archiveEntry)
    {
        var dto = new ArchivedEntityDto
        {
            Id = archiveEntry.Id,
            EntityType = archiveEntry.EntityType,
            EntityId = archiveEntry.EntityId,
            ArchivedAt = archiveEntry.ArchivedAt,
            Title = $"{archiveEntry.EntityType} (untitled)", // Default fallback
            Metadata = new Dictionary<string, object>()
        };

        // Extract title and metadata from JSON payload
        try
        {
            using var document = JsonDocument.Parse(archiveEntry.PayloadJson);
            var root = document.RootElement;

            // Extract title - check both Title and title (case-insensitive)
            if (root.TryGetProperty("Title", out var titleElement))
            {
                dto.Title = titleElement.GetString() ?? "Untitled";
            }
            else if (root.TryGetProperty("title", out var titleLowerElement))
            {
                dto.Title = titleLowerElement.GetString() ?? "Untitled";
            }

            // Extract description (if exists)
            if (root.TryGetProperty("Description", out var descElement))
            {
                dto.Description = descElement.GetString();
            }
            else if (root.TryGetProperty("description", out var descLowerElement))
            {
                dto.Description = descLowerElement.GetString();
            }
            // For Notes, try to get description from Markdown field
            if (archiveEntry.EntityType == LinkEntityType.Note && string.IsNullOrEmpty(dto.Description))
            {
                if (root.TryGetProperty("Markdown", out var markdownElement))
                {
                    var markdown = markdownElement.GetString() ?? "";
                    // Take first 200 characters as description
                    dto.Description = markdown.Length > 200
                        ? markdown.Substring(0, 200) + "..."
                        : markdown;
                }
                else if (root.TryGetProperty("markdown", out var markdownLowerElement))
                {
                    var markdown = markdownLowerElement.GetString() ?? "";
                    dto.Description = markdown.Length > 200
                        ? markdown.Substring(0, 200) + "..."
                        : markdown;
                }
            }
            // Extract entity-specific metadata
            switch (archiveEntry.EntityType)
            {
                case LinkEntityType.Note:
                    // Add character count as metadata
                    if (root.TryGetProperty("Markdown", out var noteMarkdownElement) ||
                        root.TryGetProperty("markdown", out noteMarkdownElement))
                    {
                        var markdown = noteMarkdownElement.GetString() ?? "";
                        dto.Metadata["CharacterCount"] = markdown.Length;
                    }
                    if (root.TryGetProperty("NoteGroupId", out var noteGroupElement) ||
                        root.TryGetProperty("noteGroupId", out noteGroupElement))
                    {
                        // Check if the value is not null before trying to get Guid
                        if (noteGroupElement.ValueKind != JsonValueKind.Null)
                        {
                            var groupId = noteGroupElement.GetGuid();
                            if (groupId != Guid.Empty)
                            {
                                dto.Metadata["GroupId"] = groupId.ToString();
                            }
                        }
                    }
                    break;
                case LinkEntityType.Task:
                    if (root.TryGetProperty("Status", out var statusElement))
                    {
                        if (statusElement.ValueKind == JsonValueKind.Number)
                        {
                            dto.Metadata["Status"] = statusElement.GetInt32().ToString();
                        }
                        else
                        {
                            dto.Metadata["Status"] = statusElement.GetString() ?? "";
                        }
                    }
                    if (root.TryGetProperty("Priority", out var priorityElement))
                    {
                        if (priorityElement.ValueKind == JsonValueKind.Number)
                        {
                            dto.Metadata["Priority"] = priorityElement.GetInt32().ToString();
                        }
                        else
                        {
                            dto.Metadata["Priority"] = priorityElement.GetString() ?? "";
                        }
                    }
                    if (root.TryGetProperty("DueDate", out var dueDateElement) && dueDateElement.ValueKind != JsonValueKind.Null)
                    {
                        var dueDate = dueDateElement.GetDateTime();
                        dto.Metadata["DueDate"] = dueDate.ToString("dd.MM.yyyy");
                    }
                    break;
                case LinkEntityType.Transaction:
                    if (root.TryGetProperty("Amount", out var amountElement))
                        dto.Metadata["Amount"] = amountElement.GetDecimal();
                    if (root.TryGetProperty("CurrencyCode", out var currencyElement))
                        dto.Metadata["CurrencyCode"] = currencyElement.GetString() ?? "";
                    if (root.TryGetProperty("Type", out var typeElement))
                        dto.Metadata["Type"] = typeElement.ValueKind == JsonValueKind.Number
                            ? typeElement.GetInt32()
                            : typeElement.GetString() ?? "";
                    break;

                case LinkEntityType.Budget:
                    if (root.TryGetProperty("Limit", out var limitElement))
                        dto.Metadata["Limit"] = limitElement.GetDecimal();
                    if (root.TryGetProperty("CurrencyCode", out var budgetCurrencyElement))
                        dto.Metadata["CurrencyCode"] = budgetCurrencyElement.GetString() ?? "";
                    break;

                case LinkEntityType.FinancialGoal:
                    if (root.TryGetProperty("TargetAmount", out var targetElement))
                        dto.Metadata["TargetAmount"] = targetElement.GetDecimal();
                    if (root.TryGetProperty("CurrentAmount", out var currentElement))
                        dto.Metadata["CurrentAmount"] = currentElement.GetDecimal();
                    if (root.TryGetProperty("CurrencyCode", out var goalCurrencyElement))
                        dto.Metadata["CurrencyCode"] = goalCurrencyElement.GetString() ?? "";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata from archive entry {ArchiveEntryId}. JSON: {Json}",
                archiveEntry.Id,
                archiveEntry.PayloadJson?.Substring(0, Math.Min(500, archiveEntry.PayloadJson?.Length ?? 0)));

            // Keep the default title set at the beginning instead of overwriting
            if (string.IsNullOrEmpty(dto.Title) || dto.Title.Contains("untitled"))
            {
                dto.Title = $"{archiveEntry.EntityType} (metadata unavailable)";
            }
        }
        return dto;
    }
}
