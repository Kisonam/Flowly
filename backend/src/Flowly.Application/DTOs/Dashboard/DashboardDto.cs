using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.DTOs.Dashboard;

public class DashboardDto
{

    public ActivityStatsDto ActivityStats { get; set; } = new();

    public FinanceStatsDto FinanceStats { get; set; } = new();

    public MultiCurrencyFinanceStatsDto MultiCurrencyFinanceStats { get; set; } = new();

    public List<UpcomingTaskDto> UpcomingTasks { get; set; } = new();

    public List<RecentNoteDto> RecentNotes { get; set; } = new();
}
