using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.DTOs.Dashboard;

/// <summary>
/// Dashboard overview data
/// </summary>
public class DashboardDto
{
    /// <summary>
    /// Activity statistics (tasks, notes, transactions, productivity)
    /// </summary>
    public ActivityStatsDto ActivityStats { get; set; } = new();

    /// <summary>
    /// Finance statistics for current month (legacy, single currency)
    /// </summary>
    public FinanceStatsDto FinanceStats { get; set; } = new();
    
    /// <summary>
    /// Multi-currency finance statistics for current month
    /// </summary>
    public MultiCurrencyFinanceStatsDto MultiCurrencyFinanceStats { get; set; } = new();

    /// <summary>
    /// Upcoming tasks (next 5 with due date)
    /// </summary>
    public List<UpcomingTaskDto> UpcomingTasks { get; set; } = new();

    /// <summary>
    /// Recent notes (last 3 created)
    /// </summary>
    public List<RecentNoteDto> RecentNotes { get; set; } = new();
}
