using Flowly.Application.DTOs.Dashboard;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

/// <summary>
/// Service for dashboard overview data with optimized queries
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid userId)
    {
        // Calculate current month boundaries
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

        // Execute queries sequentially (DbContext is not thread-safe)
        var activityStats = await GetActivityStatsAsync(userId, monthStart, monthEnd);
        var financeStats = await GetFinanceStatsAsync(userId, monthStart, monthEnd);
        var upcomingTasks = await GetUpcomingTasksAsync(userId);
        var recentNotes = await GetRecentNotesAsync(userId);

        return new DashboardDto
        {
            ActivityStats = activityStats,
            FinanceStats = financeStats,
            UpcomingTasks = upcomingTasks,
            RecentNotes = recentNotes
        };
    }

    /// <summary>
    /// Get activity statistics and calculate productivity score
    /// </summary>
    private async Task<ActivityStatsDto> GetActivityStatsAsync(Guid userId, DateTime monthStart, DateTime monthEnd)
    {
        // Get all tasks count
        var activeTasksCount = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && !t.IsArchived && t.Status != TasksStatus.Done)
            .CountAsync();

        var completedTasksCount = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Status == TasksStatus.Done)
            .CountAsync();

        // Get notes count
        var notesCount = await _dbContext.Notes
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsArchived)
            .CountAsync();

        // Get transactions count for this month
        var transactionsCount = await _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId 
                && !t.IsArchived
                && t.Date >= monthStart 
                && t.Date <= monthEnd)
            .CountAsync();

        // Get activity for this month
        var tasksCreatedThisMonth = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId 
                && t.CreatedAt >= monthStart 
                && t.CreatedAt <= monthEnd)
            .CountAsync();

        var tasksCompletedThisMonth = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId 
                && t.Status == TasksStatus.Done
                && t.UpdatedAt >= monthStart 
                && t.UpdatedAt <= monthEnd)
            .CountAsync();

        var notesCreatedThisMonth = await _dbContext.Notes
            .AsNoTracking()
            .Where(n => n.UserId == userId 
                && n.CreatedAt >= monthStart 
                && n.CreatedAt <= monthEnd)
            .CountAsync();

        // Calculate productivity components
        var breakdown = new ProductivityBreakdownDto
        {
            TasksCreatedThisMonth = tasksCreatedThisMonth,
            TasksCompletedThisMonth = tasksCompletedThisMonth,
            NotesCreatedThisMonth = notesCreatedThisMonth,
            TransactionsThisMonth = transactionsCount,
            
            // Task completion rate (0-100)
            TaskCompletionRate = tasksCreatedThisMonth > 0 
                ? Math.Min(100, (tasksCompletedThisMonth / (double)tasksCreatedThisMonth) * 100)
                : 0,
            
            // Notes activity score (based on notes created this month, max 100)
            NotesActivityScore = Math.Min(100, notesCreatedThisMonth * 10),
            
            // Financial tracking score (based on transactions, max 100)
            FinancialTrackingScore = Math.Min(100, transactionsCount * 5)
        };

        // Calculate overall productivity score (weighted average)
        var productivityScore = (
            breakdown.TaskCompletionRate * 0.4 +        // 40% weight
            breakdown.NotesActivityScore * 0.3 +        // 30% weight
            breakdown.FinancialTrackingScore * 0.3      // 30% weight
        );

        // Determine productivity level
        var productivityLevel = productivityScore switch
        {
            >= 80 => "Excellent",
            >= 60 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };

        return new ActivityStatsDto
        {
            ActiveTasksCount = activeTasksCount,
            CompletedTasksCount = completedTasksCount,
            NotesCount = notesCount,
            TransactionsCount = transactionsCount,
            ProductivityScore = Math.Round(productivityScore, 1),
            ProductivityLevel = productivityLevel,
            ProductivityBreakdown = breakdown
        };
    }

    /// <summary>
    /// Get finance statistics for current month
    /// Optimized: single query with aggregation
    /// </summary>
    private async Task<FinanceStatsDto> GetFinanceStatsAsync(Guid userId, DateTime periodStart, DateTime periodEnd)
    {
        // Single query to get all transactions with categories
        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.UserId == userId 
                && !t.IsArchived
                && t.Date >= periodStart 
                && t.Date <= periodEnd)
            .ToListAsync();

        // Calculate totals
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

        // Stats by month (for current month, will be single entry)
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
            CurrencyCode = "Mixed",
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

    /// <summary>
    /// Get next 5 upcoming tasks with due date
    /// Optimized: single query with filtering and ordering
    /// </summary>
    private async Task<List<UpcomingTaskDto>> GetUpcomingTasksAsync(Guid userId)
    {
        var now = DateTime.UtcNow;

        var tasks = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId 
                && !t.IsArchived
                && t.DueDate.HasValue
                && t.Status != TasksStatus.Done)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Priority)
            .Take(5)
            .Select(t => new UpcomingTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                DueDate = t.DueDate,
                Status = t.Status,
                Priority = t.Priority,
                Color = t.Color,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TasksStatus.Done
            })
            .ToListAsync();

        return tasks;
    }

    /// <summary>
    /// Get last 3 created notes
    /// Optimized: single query with ordering and limit
    /// </summary>
    private async Task<List<RecentNoteDto>> GetRecentNotesAsync(Guid userId)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsArchived)
            .OrderByDescending(n => n.CreatedAt)
            .Take(3)
            .Select(n => new RecentNoteDto
            {
                Id = n.Id,
                Title = n.Title,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync();

        return notes;
    }
}
