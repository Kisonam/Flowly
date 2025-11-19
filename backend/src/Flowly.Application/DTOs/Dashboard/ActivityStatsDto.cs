namespace Flowly.Application.DTOs.Dashboard;

/// <summary>
/// Statistics about user activities
/// </summary>
public class ActivityStatsDto
{
    /// <summary>
    /// Total number of active tasks
    /// </summary>
    public int ActiveTasksCount { get; set; }

    /// <summary>
    /// Total number of completed tasks
    /// </summary>
    public int CompletedTasksCount { get; set; }

    /// <summary>
    /// Total number of notes
    /// </summary>
    public int NotesCount { get; set; }

    /// <summary>
    /// Total number of transactions this month
    /// </summary>
    public int TransactionsCount { get; set; }

    /// <summary>
    /// Productivity score (0-100)
    /// </summary>
    public double ProductivityScore { get; set; }

    /// <summary>
    /// Productivity level: Low, Medium, High, Excellent
    /// </summary>
    public string ProductivityLevel { get; set; } = string.Empty;

    /// <summary>
    /// Breakdown of productivity components
    /// </summary>
    public ProductivityBreakdownDto ProductivityBreakdown { get; set; } = new();
}

/// <summary>
/// Detailed breakdown of productivity calculation
/// </summary>
public class ProductivityBreakdownDto
{
    /// <summary>
    /// Task completion rate (0-100)
    /// </summary>
    public double TaskCompletionRate { get; set; }

    /// <summary>
    /// Notes activity score (0-100)
    /// </summary>
    public double NotesActivityScore { get; set; }

    /// <summary>
    /// Financial tracking score (0-100)
    /// </summary>
    public double FinancialTrackingScore { get; set; }

    /// <summary>
    /// Number of tasks created this month
    /// </summary>
    public int TasksCreatedThisMonth { get; set; }

    /// <summary>
    /// Number of tasks completed this month
    /// </summary>
    public int TasksCompletedThisMonth { get; set; }

    /// <summary>
    /// Number of notes created this month
    /// </summary>
    public int NotesCreatedThisMonth { get; set; }

    /// <summary>
    /// Number of transactions this month
    /// </summary>
    public int TransactionsThisMonth { get; set; }
}
