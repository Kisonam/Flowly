namespace Flowly.Application.DTOs.Transactions;

public class MultiCurrencyFinanceStatsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalTransactionCount { get; set; }

    public List<CurrencyStatsDto> ByCurrency { get; set; } = new();

    public List<MonthStatsDto> ByMonth { get; set; } = new();

    public List<string> AvailableCurrencies { get; set; } = new();
}
