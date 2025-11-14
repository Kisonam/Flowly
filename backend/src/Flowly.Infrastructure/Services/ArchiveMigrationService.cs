using Flowly.Application.Interfaces;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Flowly.Infrastructure.Services;

/// <summary>
/// Service for migrating existing archived entities to the new archive system
/// </summary>
public class ArchiveMigrationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ArchiveMigrationService> _logger;

    public ArchiveMigrationService(AppDbContext dbContext, ILogger<ArchiveMigrationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Migrate all existing archived entities to ArchiveEntries table
    /// </summary>
    public async Task MigrateExistingArchivedEntitiesAsync()
    {
        _logger.LogInformation("Starting migration of existing archived entities...");

        var migratedCount = 0;

        // Migrate archived Notes
        var archivedNotes = await _dbContext.Notes
            .Include(n => n.NoteTags)
            .Include(n => n.MediaAssets)
            .Where(n => n.IsArchived)
            .ToListAsync();

        foreach (var note in archivedNotes)
        {
            // Check if archive entry already exists
            var exists = await _dbContext.ArchiveEntries
                .AnyAsync(a => a.EntityType == LinkEntityType.Note && a.EntityId == note.Id);

            if (!exists)
            {
                var archiveEntry = new Domain.Entities.ArchiveEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = note.UserId,
                    EntityType = LinkEntityType.Note,
                    EntityId = note.Id,
                    PayloadJson = SerializeEntity(note),
                    ArchivedAt = note.UpdatedAt // Use UpdatedAt as best approximation
                };

                _dbContext.ArchiveEntries.Add(archiveEntry);
                migratedCount++;
            }
        }

        // Migrate archived Tasks
        var archivedTasks = await _dbContext.Tasks
            .Include(t => t.Subtasks)
            .Include(t => t.TaskTags)
            .Where(t => t.IsArchived)
            .ToListAsync();

        foreach (var task in archivedTasks)
        {
            var exists = await _dbContext.ArchiveEntries
                .AnyAsync(a => a.EntityType == LinkEntityType.Task && a.EntityId == task.Id);

            if (!exists)
            {
                var archiveEntry = new Domain.Entities.ArchiveEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = task.UserId,
                    EntityType = LinkEntityType.Task,
                    EntityId = task.Id,
                    PayloadJson = SerializeEntity(task),
                    ArchivedAt = task.UpdatedAt
                };

                _dbContext.ArchiveEntries.Add(archiveEntry);
                migratedCount++;
            }
        }

        // Migrate archived Transactions
        var archivedTransactions = await _dbContext.Transactions
            .Include(t => t.TransactionTags)
            .Where(t => t.IsArchived)
            .ToListAsync();

        foreach (var transaction in archivedTransactions)
        {
            var exists = await _dbContext.ArchiveEntries
                .AnyAsync(a => a.EntityType == LinkEntityType.Transaction && a.EntityId == transaction.Id);

            if (!exists)
            {
                var archiveEntry = new Domain.Entities.ArchiveEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = transaction.UserId,
                    EntityType = LinkEntityType.Transaction,
                    EntityId = transaction.Id,
                    PayloadJson = SerializeEntity(transaction),
                    ArchivedAt = transaction.UpdatedAt
                };

                _dbContext.ArchiveEntries.Add(archiveEntry);
                migratedCount++;
            }
        }

        // Migrate archived Budgets
        var archivedBudgets = await _dbContext.Budgets
            .Where(b => b.IsArchived)
            .ToListAsync();

        foreach (var budget in archivedBudgets)
        {
            var exists = await _dbContext.ArchiveEntries
                .AnyAsync(a => a.EntityType == LinkEntityType.Budget && a.EntityId == budget.Id);

            if (!exists)
            {
                var archiveEntry = new Domain.Entities.ArchiveEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = budget.UserId,
                    EntityType = LinkEntityType.Budget,
                    EntityId = budget.Id,
                    PayloadJson = SerializeEntity(budget),
                    ArchivedAt = budget.ArchivedAt ?? budget.UpdatedAt ?? budget.CreatedAt
                };

                _dbContext.ArchiveEntries.Add(archiveEntry);
                migratedCount++;
            }
        }

        // Migrate archived FinancialGoals
        var archivedGoals = await _dbContext.FinancialGoals
            .Where(g => g.IsArchived)
            .ToListAsync();

        foreach (var goal in archivedGoals)
        {
            var exists = await _dbContext.ArchiveEntries
                .AnyAsync(a => a.EntityType == LinkEntityType.FinancialGoal && a.EntityId == goal.Id);

            if (!exists)
            {
                var archiveEntry = new Domain.Entities.ArchiveEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = goal.UserId,
                    EntityType = LinkEntityType.FinancialGoal,
                    EntityId = goal.Id,
                    PayloadJson = SerializeEntity(goal),
                    ArchivedAt = goal.UpdatedAt
                };

                _dbContext.ArchiveEntries.Add(archiveEntry);
                migratedCount++;
            }
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Migration completed. Migrated {Count} archived entities to ArchiveEntries", migratedCount);
    }

    private string SerializeEntity(object entity)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        return JsonSerializer.Serialize(entity, options);
    }
}
