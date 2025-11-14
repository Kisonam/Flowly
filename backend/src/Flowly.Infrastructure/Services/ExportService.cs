// backend/src/Flowly.Infrastructure/Services/ExportService.cs

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Flowly.Application.DTOs.Export;
using Flowly.Application.Interfaces;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Flowly.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _dbContext;

    public ExportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<byte[]> ExportAsMarkdownZipAsync(string userId)
    {
        var data = await GetUserDataAsync(userId);
        
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // README
            await AddTextFileToArchive(archive, "README.md", GenerateReadme(data));

            // Notes
            if (data.Notes.Any())
            {
                foreach (var note in data.Notes)
                {
                    var fileName = SanitizeFileName($"notes/{note.Title}.md");
                    var content = GenerateNoteMarkdown(note);
                    await AddTextFileToArchive(archive, fileName, content);
                }
            }

            // Tasks
            if (data.Tasks.Any())
            {
                var tasksContent = GenerateTasksMarkdown(data.Tasks);
                await AddTextFileToArchive(archive, "tasks/tasks.md", tasksContent);
            }

            // Transactions
            if (data.Transactions.Any())
            {
                var transactionsContent = GenerateTransactionsMarkdown(data.Transactions);
                await AddTextFileToArchive(archive, "finance/transactions.md", transactionsContent);
            }

            // Budgets
            if (data.Budgets.Any())
            {
                var budgetsContent = GenerateBudgetsMarkdown(data.Budgets);
                await AddTextFileToArchive(archive, "finance/budgets.md", budgetsContent);
            }

            // Goals
            if (data.Goals.Any())
            {
                var goalsContent = GenerateGoalsMarkdown(data.Goals);
                await AddTextFileToArchive(archive, "finance/goals.md", goalsContent);
            }

            // Categories & Tags
            var metadataContent = GenerateMetadataMarkdown(data);
            await AddTextFileToArchive(archive, "metadata.md", metadataContent);
        }

        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportAsJsonAsync(string userId)
    {
        var data = await GetUserDataAsync(userId);
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(data, options);
        return Encoding.UTF8.GetBytes(json);
    }

    public async Task<byte[]> ExportAsCsvAsync(string userId)
    {
        var data = await GetUserDataAsync(userId);
        
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Notes CSV
            if (data.Notes.Any())
            {
                var notesCsv = GenerateNotesCsv(data.Notes);
                await AddTextFileToArchive(archive, "notes.csv", notesCsv);
            }

            // Tasks CSV
            if (data.Tasks.Any())
            {
                var tasksCsv = GenerateTasksCsv(data.Tasks);
                await AddTextFileToArchive(archive, "tasks.csv", tasksCsv);
            }

            // Transactions CSV
            if (data.Transactions.Any())
            {
                var transactionsCsv = GenerateTransactionsCsv(data.Transactions);
                await AddTextFileToArchive(archive, "transactions.csv", transactionsCsv);
            }

            // Budgets CSV
            if (data.Budgets.Any())
            {
                var budgetsCsv = GenerateBudgetsCsv(data.Budgets);
                await AddTextFileToArchive(archive, "budgets.csv", budgetsCsv);
            }

            // Goals CSV
            if (data.Goals.Any())
            {
                var goalsCsv = GenerateGoalsCsv(data.Goals);
                await AddTextFileToArchive(archive, "goals.csv", goalsCsv);
            }
        }

        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportAsPdfAsync(string userId)
    {
        var data = await GetUserDataAsync(userId);

        // Set QuestPDF license type (required)
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        using var ms = new MemoryStream();
        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Text("Flowly Data Export").SemiBold().FontSize(20).FontColor("#2563eb");

                page.Content().Column(col =>
                {
                    col.Item().Text($"User: {data.User.DisplayName} ({data.User.Email})");
                    col.Item().Text($"Export Date: {data.ExportedAt:yyyy-MM-dd HH:mm:ss} UTC");
                    col.Item().PaddingVertical(8);
                    col.Item().Text($"Notes: {data.Notes.Count}");
                    col.Item().Text($"Tasks: {data.Tasks.Count}");
                    col.Item().Text($"Transactions: {data.Transactions.Count}");
                    col.Item().Text($"Budgets: {data.Budgets.Count}");
                    col.Item().Text($"Goals: {data.Goals.Count}");
                    col.Item().PaddingVertical(8);

                    // Notes Table
                    if (data.Notes.Any())
                    {
                        col.Item().PaddingTop(10).Text("Notes").Bold().FontSize(16).FontColor("#1d4ed8");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Title
                                c.RelativeColumn(3); // Content
                                c.RelativeColumn(1); // Group
                                c.RelativeColumn(2); // Tags
                                c.RelativeColumn(1); // Created
                                c.RelativeColumn(1); // Updated
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Title").SemiBold();
                                h.Cell().Element(CellStyle).Text("Content").SemiBold();
                                h.Cell().Element(CellStyle).Text("Group").SemiBold();
                                h.Cell().Element(CellStyle).Text("Tags").SemiBold();
                                h.Cell().Element(CellStyle).Text("Created").SemiBold();
                                h.Cell().Element(CellStyle).Text("Updated").SemiBold();
                            });
                            foreach (var n in data.Notes)
                            {
                                table.Cell().Element(CellStyle).Text(n.Title);
                                table.Cell().Element(CellStyle).Text(n.Content.Length > 100 ? n.Content.Substring(0, 100) + "..." : n.Content);
                                table.Cell().Element(CellStyle).Text(n.GroupName ?? "");
                                table.Cell().Element(CellStyle).Text(string.Join(", ", n.Tags));
                                table.Cell().Element(CellStyle).Text(n.CreatedAt.ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text(n.UpdatedAt.ToString("yyyy-MM-dd"));
                            }
                        });
                    }

                    // Tasks Table
                    if (data.Tasks.Any())
                    {
                        col.Item().PaddingTop(10).Text("Tasks").Bold().FontSize(16).FontColor("#1d4ed8");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Title
                                c.RelativeColumn(2); // Description
                                c.RelativeColumn(1); // Status
                                c.RelativeColumn(1); // Priority
                                c.RelativeColumn(1); // Theme
                                c.RelativeColumn(1); // DueDate
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Title").SemiBold();
                                h.Cell().Element(CellStyle).Text("Description").SemiBold();
                                h.Cell().Element(CellStyle).Text("Status").SemiBold();
                                h.Cell().Element(CellStyle).Text("Priority").SemiBold();
                                h.Cell().Element(CellStyle).Text("Theme").SemiBold();
                                h.Cell().Element(CellStyle).Text("DueDate").SemiBold();
                            });
                            foreach (var t in data.Tasks)
                            {
                                table.Cell().Element(CellStyle).Text(t.Title);
                                table.Cell().Element(CellStyle).Text(t.Description ?? "");
                                table.Cell().Element(CellStyle).Text(t.Status);
                                table.Cell().Element(CellStyle).Text(t.Priority);
                                table.Cell().Element(CellStyle).Text(t.Theme ?? "");
                                table.Cell().Element(CellStyle).Text(t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "");
                            }
                        });
                    }

                    // Transactions Table
                    if (data.Transactions.Any())
                    {
                        col.Item().PaddingTop(10).Text("Transactions").Bold().FontSize(16).FontColor("#1d4ed8");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1); // Type
                                c.RelativeColumn(1); // Amount
                                c.RelativeColumn(1); // Currency
                                c.RelativeColumn(2); // Description
                                c.RelativeColumn(1); // Category
                                c.RelativeColumn(2); // Tags
                                c.RelativeColumn(1); // Date
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Type").SemiBold();
                                h.Cell().Element(CellStyle).Text("Amount").SemiBold();
                                h.Cell().Element(CellStyle).Text("Currency").SemiBold();
                                h.Cell().Element(CellStyle).Text("Description").SemiBold();
                                h.Cell().Element(CellStyle).Text("Category").SemiBold();
                                h.Cell().Element(CellStyle).Text("Tags").SemiBold();
                                h.Cell().Element(CellStyle).Text("Date").SemiBold();
                            });
                            foreach (var tr in data.Transactions)
                            {
                                table.Cell().Element(CellStyle).Text(tr.Type);
                                table.Cell().Element(CellStyle).Text(tr.Amount.ToString("F2"));
                                table.Cell().Element(CellStyle).Text(tr.Currency);
                                table.Cell().Element(CellStyle).Text(tr.Description ?? "");
                                table.Cell().Element(CellStyle).Text(tr.Category ?? "");
                                table.Cell().Element(CellStyle).Text(string.Join(", ", tr.Tags));
                                table.Cell().Element(CellStyle).Text(tr.Date.ToString("yyyy-MM-dd"));
                            }
                        });
                    }

                    // Budgets Table
                    if (data.Budgets.Any())
                    {
                        col.Item().PaddingTop(10).Text("Budgets").Bold().FontSize(16).FontColor("#1d4ed8");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Name
                                c.RelativeColumn(1); // Amount
                                c.RelativeColumn(1); // Currency
                                c.RelativeColumn(1); // Period
                                c.RelativeColumn(1); // Category
                                c.RelativeColumn(1); // StartDate
                                c.RelativeColumn(1); // EndDate
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Name").SemiBold();
                                h.Cell().Element(CellStyle).Text("Amount").SemiBold();
                                h.Cell().Element(CellStyle).Text("Currency").SemiBold();
                                h.Cell().Element(CellStyle).Text("Period").SemiBold();
                                h.Cell().Element(CellStyle).Text("Category").SemiBold();
                                h.Cell().Element(CellStyle).Text("StartDate").SemiBold();
                                h.Cell().Element(CellStyle).Text("EndDate").SemiBold();
                            });
                            foreach (var b in data.Budgets)
                            {
                                table.Cell().Element(CellStyle).Text(b.Name);
                                table.Cell().Element(CellStyle).Text(b.Amount.ToString("F2"));
                                table.Cell().Element(CellStyle).Text(b.Currency);
                                table.Cell().Element(CellStyle).Text(b.Period);
                                table.Cell().Element(CellStyle).Text(b.Category ?? "");
                                table.Cell().Element(CellStyle).Text(b.StartDate.ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text(b.EndDate.ToString("yyyy-MM-dd"));
                            }
                        });
                    }

                    // Goals Table
                    if (data.Goals.Any())
                    {
                        col.Item().PaddingTop(10).Text("Goals").Bold().FontSize(16).FontColor("#1d4ed8");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Name
                                c.RelativeColumn(1); // TargetAmount
                                c.RelativeColumn(1); // CurrentAmount
                                c.RelativeColumn(1); // Currency
                                c.RelativeColumn(1); // Status
                                c.RelativeColumn(1); // TargetDate
                                c.RelativeColumn(1); // Created
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Name").SemiBold();
                                h.Cell().Element(CellStyle).Text("TargetAmount").SemiBold();
                                h.Cell().Element(CellStyle).Text("CurrentAmount").SemiBold();
                                h.Cell().Element(CellStyle).Text("Currency").SemiBold();
                                h.Cell().Element(CellStyle).Text("Status").SemiBold();
                                h.Cell().Element(CellStyle).Text("TargetDate").SemiBold();
                                h.Cell().Element(CellStyle).Text("Created").SemiBold();
                            });
                            foreach (var g in data.Goals)
                            {
                                table.Cell().Element(CellStyle).Text(g.Name);
                                table.Cell().Element(CellStyle).Text(g.TargetAmount.ToString("F2"));
                                table.Cell().Element(CellStyle).Text(g.CurrentAmount.ToString("F2"));
                                table.Cell().Element(CellStyle).Text(g.Currency);
                                table.Cell().Element(CellStyle).Text(g.Status);
                                table.Cell().Element(CellStyle).Text(g.TargetDate.HasValue ? g.TargetDate.Value.ToString("yyyy-MM-dd") : "");
                                table.Cell().Element(CellStyle).Text(g.CreatedAt.ToString("yyyy-MM-dd"));
                            }
                        });
                    }

                    // Categories Table
                    if (data.Categories.Any())
                    {
                        col.Item().PaddingTop(10).Text("Categories").Bold().FontSize(16).FontColor("#1d4ed8");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Name
                                c.RelativeColumn(1); // Type
                                c.RelativeColumn(1); // Color
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Name").SemiBold();
                                h.Cell().Element(CellStyle).Text("Type").SemiBold();
                                h.Cell().Element(CellStyle).Text("Color").SemiBold();
                            });
                            foreach (var c in data.Categories)
                            {
                                table.Cell().Element(CellStyle).Text(c.Name);
                                table.Cell().Element(CellStyle).Text(c.Type);
                                table.Cell().Element(CellStyle).Text(c.Color);
                            }
                        });
                    }

                    // Tags Table
                    if (data.Tags.Any())
                    {
                        col.Item().PaddingTop(10).Text("Tags").Bold().FontSize(16).FontColor("#1d4ed8");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Name
                                c.RelativeColumn(1); // Color
                            });
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("Name").SemiBold();
                                h.Cell().Element(CellStyle).Text("Color").SemiBold();
                            });
                            foreach (var t in data.Tags)
                            {
                                table.Cell().Element(CellStyle).Text(t.Name);
                                table.Cell().Element(CellStyle).Text(t.Color);
                            }
                        });
                    }
                });
            });
        }).GeneratePdf(ms);

        return ms.ToArray();

        // Local function for cell style
        IContainer CellStyle(IContainer container) => container.Padding(2).BorderBottom(0.5f).BorderColor("#e5e7eb");
    }

    #region Private Helper Methods

    private async Task<ExportDataDto> GetUserDataAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);

        // Get user
        var user = await _dbContext.Users.FindAsync(userGuid);

        // Get notes
        var notes = await _dbContext.Notes
            .Include(n => n.NoteGroup)
            .Include(n => n.NoteTags).ThenInclude(nt => nt.Tag)
            .Where(n => n.UserId == userGuid)
            .Select(n => new NoteExportDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Markdown,
                GroupName = n.NoteGroup != null ? n.NoteGroup.Title : null,
                Tags = n.NoteTags.Select(nt => nt.Tag.Name).ToList(),
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync();

        // Get tasks
        var tasks = await _dbContext.Tasks
            .Include(t => t.TaskTheme)
            .Where(t => t.UserId == userGuid)
            .Select(t => new TaskExportDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Theme = t.TaskTheme != null ? t.TaskTheme.Title : null,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        // Get transactions
        var transactions = await _dbContext.Transactions
            .Include(t => t.Category)
            .Include(t => t.Currency)
            .Include(t => t.TransactionTags).ThenInclude(tt => tt.Tag)
            .Where(t => t.UserId == userGuid)
            .Select(t => new TransactionExportDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Currency = t.Currency.Code,
                Description = t.Description,
                Category = t.Category != null ? t.Category.Name : null,
                Tags = t.TransactionTags.Select(tt => tt.Tag.Name).ToList(),
                Date = t.Date,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        // Get budgets
        var budgets = await _dbContext.Budgets
            .Include(b => b.Category)
            .Include(b => b.Currency)
            .Where(b => b.UserId == userGuid)
            .Select(b => new BudgetExportDto
            {
                Id = b.Id,
                Name = b.Title,
                Amount = b.Limit,
                Currency = b.Currency.Code,
                Period = "Custom", // Budget doesn't have Period enum, just dates
                Category = b.Category != null ? b.Category.Name : null,
                StartDate = b.PeriodStart,
                EndDate = b.PeriodEnd
            })
            .ToListAsync();

        // Get goals
        var goalsQuery = await _dbContext.FinancialGoals
            .Include(g => g.Currency)
            .Where(g => g.UserId == userGuid)
            .ToListAsync();

        var goals = goalsQuery.Select(g => new GoalExportDto
        {
            Id = g.Id,
            Name = g.Title,
            TargetAmount = g.TargetAmount,
            CurrentAmount = g.CurrentAmount,
            Currency = g.Currency.Code,
            TargetDate = g.Deadline,
            Status = g.IsCompleted() ? "Completed" : (g.IsOverdue() ? "Overdue" : "InProgress"),
            CreatedAt = g.CreatedAt
        }).ToList();

        // Get categories
        var categories = await _dbContext.Categories
            .Where(c => c.UserId == userGuid)
            .Select(c => new CategoryExportDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = "General", // Category doesn't have Type field
                Color = c.Color ?? "#808080"
            })
            .ToListAsync();

        // Get tags
        var tags = await _dbContext.Tags
            .Where(t => t.UserId == userGuid)
            .Select(t => new TagExportDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color ?? "#808080"
            })
            .ToListAsync();

        return new ExportDataDto
        {
            User = new UserExportDto
            {
                Id = user!.Id.ToString(),
                DisplayName = user.DisplayName,
                Email = user.Email ?? "no-email@flowly.app"
            },
            Notes = notes,
            Tasks = tasks,
            Transactions = transactions,
            Budgets = budgets,
            Goals = goals,
            Categories = categories,
            Tags = tags,
            ExportedAt = DateTime.UtcNow
        };
    }

    private static async Task AddTextFileToArchive(ZipArchive archive, string fileName, string content)
    {
        var entry = archive.CreateEntry(fileName, CompressionLevel.NoCompression);
        await using var entryStream = entry.Open();
        
        // Write UTF-8 BOM for better Excel compatibility
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        await entryStream.WriteAsync(bom, 0, bom.Length);
        
        // Write content with UTF-8 encoding (without additional BOM since we already wrote it)
        var encoding = new UTF8Encoding(false);
        var bytes = encoding.GetBytes(content);
        await entryStream.WriteAsync(bytes, 0, bytes.Length);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Split path into directory and filename
        var lastSlashIndex = fileName.LastIndexOf('/');
        if (lastSlashIndex >= 0)
        {
            var directory = fileName.Substring(0, lastSlashIndex + 1);
            var filename = fileName.Substring(lastSlashIndex + 1);
            
            // Sanitize only the filename part, keep directory structure
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", filename.Split(invalid));
            
            // Limit filename length (not the full path)
            if (sanitized.Length > 150)
                sanitized = sanitized.Substring(0, 150) + ".md";
            
            return directory + sanitized;
        }
        else
        {
            // No directory, just sanitize the filename
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalid));
            return sanitized.Length > 200 ? sanitized.Substring(0, 200) : sanitized;
        }
    }

    private static string GenerateReadme(ExportDataDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Flowly Data Export");
        sb.AppendLine();
        sb.AppendLine($"**User:** {data.User.DisplayName} ({data.User.Email})");
        sb.AppendLine($"**Export Date:** {data.ExportedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Notes:** {data.Notes.Count}");
        sb.AppendLine($"- **Tasks:** {data.Tasks.Count}");
        sb.AppendLine($"- **Transactions:** {data.Transactions.Count}");
        sb.AppendLine($"- **Budgets:** {data.Budgets.Count}");
        sb.AppendLine($"- **Goals:** {data.Goals.Count}");
        sb.AppendLine($"- **Categories:** {data.Categories.Count}");
        sb.AppendLine($"- **Tags:** {data.Tags.Count}");
        sb.AppendLine();
        sb.AppendLine("## Contents");
        sb.AppendLine();
        sb.AppendLine("- `notes/` - All your notes in Markdown format");
        sb.AppendLine("- `tasks/` - Your tasks");
        sb.AppendLine("- `finance/` - Financial data (transactions, budgets, goals)");
        sb.AppendLine("- `metadata.md` - Categories and tags");
        
        return sb.ToString();
    }

    private static string GenerateNoteMarkdown(NoteExportDto note)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {note.Title}");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(note.GroupName))
        {
            sb.AppendLine($"**Group:** {note.GroupName}");
        }
        
        if (note.Tags.Any())
        {
            sb.AppendLine($"**Tags:** {string.Join(", ", note.Tags)}");
        }
        
        sb.AppendLine($"**Created:** {note.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"**Updated:** {note.UpdatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(note.Content);
        
        return sb.ToString();
    }

    private static string GenerateTasksMarkdown(List<TaskExportDto> tasks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Tasks");
        sb.AppendLine();
        
        var grouped = tasks.GroupBy(t => t.Status);
        
        foreach (var group in grouped)
        {
            sb.AppendLine($"## {group.Key}");
            sb.AppendLine();
            
            foreach (var task in group.OrderByDescending(t => t.CreatedAt))
            {
                sb.AppendLine($"### {task.Title}");
                
                if (!string.IsNullOrEmpty(task.Description))
                {
                    sb.AppendLine(task.Description);
                    sb.AppendLine();
                }
                
                sb.AppendLine($"- **Priority:** {task.Priority}");
                if (task.DueDate.HasValue)
                {
                    sb.AppendLine($"- **Due Date:** {task.DueDate.Value:yyyy-MM-dd}");
                }
                if (!string.IsNullOrEmpty(task.Theme))
                {
                    sb.AppendLine($"- **Theme:** {task.Theme}");
                }
                sb.AppendLine($"- **Created:** {task.CreatedAt:yyyy-MM-dd HH:mm}");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }

    private static string GenerateTransactionsMarkdown(List<TransactionExportDto> transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Transactions");
        sb.AppendLine();
        
        var byMonth = transactions.GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month);
        
        foreach (var monthGroup in byMonth)
        {
            sb.AppendLine($"## {new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1):MMMM yyyy}");
            sb.AppendLine();
            
            foreach (var transaction in monthGroup.OrderByDescending(t => t.Date))
            {
                var sign = transaction.Type == "Income" ? "+" : "-";
                sb.AppendLine($"- **{transaction.Date:dd MMM}** - {transaction.Description ?? "No description"} - {sign}{transaction.Amount:N2} {transaction.Currency}");
                
                if (!string.IsNullOrEmpty(transaction.Category))
                {
                    sb.AppendLine($"  - Category: {transaction.Category}");
                }
                
                if (transaction.Tags.Any())
                {
                    sb.AppendLine($"  - Tags: {string.Join(", ", transaction.Tags)}");
                }
                
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }

    private static string GenerateBudgetsMarkdown(List<BudgetExportDto> budgets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Budgets");
        sb.AppendLine();
        
        foreach (var budget in budgets.OrderBy(b => b.Name))
        {
            sb.AppendLine($"## {budget.Name}");
            sb.AppendLine($"- **Amount:** {budget.Amount:N2} {budget.Currency}");
            sb.AppendLine($"- **Period:** {budget.Period}");
            
            if (!string.IsNullOrEmpty(budget.Category))
            {
                sb.AppendLine($"- **Category:** {budget.Category}");
            }
            
            sb.AppendLine($"- **Date Range:** {budget.StartDate:yyyy-MM-dd} to {budget.EndDate:yyyy-MM-dd}");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private static string GenerateGoalsMarkdown(List<GoalExportDto> goals)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Financial Goals");
        sb.AppendLine();
        
        foreach (var goal in goals.OrderBy(g => g.Name))
        {
            var progress = goal.TargetAmount > 0 ? (goal.CurrentAmount / goal.TargetAmount * 100) : 0;
            
            sb.AppendLine($"## {goal.Name}");
            sb.AppendLine($"- **Progress:** {goal.CurrentAmount:N2} / {goal.TargetAmount:N2} {goal.Currency} ({progress:F1}%)");
            sb.AppendLine($"- **Status:** {goal.Status}");
            
            if (goal.TargetDate.HasValue)
            {
                sb.AppendLine($"- **Target Date:** {goal.TargetDate.Value:yyyy-MM-dd}");
            }
            
            sb.AppendLine($"- **Created:** {goal.CreatedAt:yyyy-MM-dd}");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private static string GenerateMetadataMarkdown(ExportDataDto data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Metadata");
        sb.AppendLine();
        
        if (data.Categories.Any())
        {
            sb.AppendLine("## Categories");
            sb.AppendLine();
            
            foreach (var category in data.Categories.OrderBy(c => c.Name))
            {
                sb.AppendLine($"- **{category.Name}** ({category.Type}) - Color: {category.Color}");
            }
            
            sb.AppendLine();
        }
        
        if (data.Tags.Any())
        {
            sb.AppendLine("## Tags");
            sb.AppendLine();
            
            foreach (var tag in data.Tags.OrderBy(t => t.Name))
            {
                sb.AppendLine($"- **{tag.Name}** - Color: {tag.Color}");
            }
        }
        
        return sb.ToString();
    }

    // CSV Generators
    private static string GenerateNotesCsv(List<NoteExportDto> notes)
    {
        var sb = new StringBuilder();
        sb.Append("Id,Title,Group,Tags,Created,Updated\r\n");
        
        foreach (var note in notes)
        {
            var tags = note.Tags.Any() ? string.Join("; ", note.Tags) : "";
            sb.Append(note.Id.ToString());
            sb.Append(",\"");
            sb.Append(EscapeCsv(note.Title));
            sb.Append("\",\"");
            sb.Append(EscapeCsv(note.GroupName));
            sb.Append("\",\"");
            sb.Append(tags);
            sb.Append("\",");
            sb.Append(note.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
            sb.Append(",");
            sb.Append(note.UpdatedAt.ToString("yyyy-MM-dd HH:mm"));
            sb.Append("\r\n");
        }
        
        return sb.ToString();
    }

    private static string GenerateTasksCsv(List<TaskExportDto> tasks)
    {
        var sb = new StringBuilder();
        sb.Append("Id,Title,Status,Priority,Theme,DueDate,Created\r\n");
        
        foreach (var task in tasks)
        {
            var dueDate = task.DueDate.HasValue ? task.DueDate.Value.ToString("yyyy-MM-dd") : "";
            sb.Append(task.Id.ToString());
            sb.Append(",\"");
            sb.Append(EscapeCsv(task.Title));
            sb.Append("\",");
            sb.Append(task.Status);
            sb.Append(",");
            sb.Append(task.Priority);
            sb.Append(",\"");
            sb.Append(EscapeCsv(task.Theme));
            sb.Append("\",");
            sb.Append(dueDate);
            sb.Append(",");
            sb.Append(task.CreatedAt.ToString("yyyy-MM-dd"));
            sb.Append("\r\n");
        }
        
        return sb.ToString();
    }

    private static string GenerateTransactionsCsv(List<TransactionExportDto> transactions)
    {
        var sb = new StringBuilder();
        sb.Append("Id,Type,Amount,Currency,Category,Description,Tags,Date\r\n");
        
        foreach (var t in transactions)
        {
            var tags = t.Tags.Any() ? string.Join("; ", t.Tags) : "";
            sb.Append(t.Id.ToString());
            sb.Append(",");
            sb.Append(t.Type);
            sb.Append(",");
            sb.Append(t.Amount.ToString("F2"));
            sb.Append(",");
            sb.Append(t.Currency);
            sb.Append(",\"");
            sb.Append(EscapeCsv(t.Category));
            sb.Append("\",\"");
            sb.Append(EscapeCsv(t.Description));
            sb.Append("\",\"");
            sb.Append(tags);
            sb.Append("\",");
            sb.Append(t.Date.ToString("yyyy-MM-dd"));
            sb.Append("\r\n");
        }
        
        return sb.ToString();
    }

    private static string GenerateBudgetsCsv(List<BudgetExportDto> budgets)
    {
        var sb = new StringBuilder();
        sb.Append("Id,Name,Amount,Currency,Period,Category,StartDate,EndDate\r\n");
        
        foreach (var b in budgets)
        {
            sb.Append(b.Id.ToString());
            sb.Append(",\"");
            sb.Append(EscapeCsv(b.Name));
            sb.Append("\",");
            sb.Append(b.Amount.ToString("F2"));
            sb.Append(",");
            sb.Append(b.Currency);
            sb.Append(",");
            sb.Append(b.Period);
            sb.Append(",\"");
            sb.Append(EscapeCsv(b.Category));
            sb.Append("\",");
            sb.Append(b.StartDate.ToString("yyyy-MM-dd"));
            sb.Append(",");
            sb.Append(b.EndDate.ToString("yyyy-MM-dd"));
            sb.Append("\r\n");
        }
        
        return sb.ToString();
    }

    private static string GenerateGoalsCsv(List<GoalExportDto> goals)
    {
        var sb = new StringBuilder();
        sb.Append("Id,Name,TargetAmount,CurrentAmount,Currency,Status,TargetDate,Created\r\n");
        
        foreach (var g in goals)
        {
            var targetDate = g.TargetDate.HasValue ? g.TargetDate.Value.ToString("yyyy-MM-dd") : "";
            sb.Append(g.Id.ToString());
            sb.Append(",\"");
            sb.Append(EscapeCsv(g.Name));
            sb.Append("\",");
            sb.Append(g.TargetAmount.ToString("F2"));
            sb.Append(",");
            sb.Append(g.CurrentAmount.ToString("F2"));
            sb.Append(",");
            sb.Append(g.Currency);
            sb.Append(",");
            sb.Append(g.Status);
            sb.Append(",");
            sb.Append(targetDate);
            sb.Append(",");
            sb.Append(g.CreatedAt.ToString("yyyy-MM-dd"));
            sb.Append("\r\n");
        }
        
        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
            
        return value.Replace("\"", "\"\"");
    }

    #endregion
}
