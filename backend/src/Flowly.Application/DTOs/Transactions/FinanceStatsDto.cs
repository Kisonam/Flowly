namespace Flowly.Application.DTOs.Transactions;

public class FinanceStatsDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public List<CategoryStatsDto> IncomeByCategory { get; set; } = new();
    public List<CategoryStatsDto> ExpenseByCategory { get; set; } = new();

    public List<MonthStatsDto> ByMonth { get; set; } = new();

    public decimal AverageDailyIncome { get; set; }
    public decimal AverageDailyExpense { get; set; }
    public int TotalTransactionCount { get; set; }
}
