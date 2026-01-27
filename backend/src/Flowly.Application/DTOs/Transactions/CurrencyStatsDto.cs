namespace Flowly.Application.DTOs.Transactions;

public class CurrencyStatsDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }

    public List<CategoryStatsDto> IncomeByCategory { get; set; } = new();
    public List<CategoryStatsDto> ExpenseByCategory { get; set; } = new();
}
