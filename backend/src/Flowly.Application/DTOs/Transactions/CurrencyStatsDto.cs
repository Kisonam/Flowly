namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Financial statistics for a specific currency
/// </summary>
public class CurrencyStatsDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
    
    // Statistics by category for this currency
    public List<CategoryStatsDto> IncomeByCategory { get; set; } = new();
    public List<CategoryStatsDto> ExpenseByCategory { get; set; } = new();
}
