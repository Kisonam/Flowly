namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Multi-currency financial statistics DTO
/// </summary>
public class MultiCurrencyFinanceStatsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalTransactionCount { get; set; }
    
    /// <summary>
    /// Statistics grouped by currency
    /// </summary>
    public List<CurrencyStatsDto> ByCurrency { get; set; } = new();
    
    /// <summary>
    /// Statistics by month (aggregated across all currencies)
    /// </summary>
    public List<MonthStatsDto> ByMonth { get; set; } = new();
    
    /// <summary>
    /// List of all currencies present in the data
    /// </summary>
    public List<string> AvailableCurrencies { get; set; } = new();
}
