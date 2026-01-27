namespace Flowly.Application.DTOs.Dashboard;

public class ActivityStatsDto
{

    public int ActiveTasksCount { get; set; }

    public int CompletedTasksCount { get; set; }

    public int NotesCount { get; set; }

    public int TransactionsCount { get; set; }

    public double ProductivityScore { get; set; }

    public string ProductivityLevel { get; set; } = string.Empty;

    public ProductivityBreakdownDto ProductivityBreakdown { get; set; } = new();
}

public class ProductivityBreakdownDto
{

    public double TaskCompletionRate { get; set; }

    public double NotesActivityScore { get; set; }

    public double FinancialTrackingScore { get; set; }

    public int TasksCreatedThisMonth { get; set; }

    public int TasksCompletedThisMonth { get; set; }

    public int NotesCreatedThisMonth { get; set; }

    public int TransactionsThisMonth { get; set; }
}
