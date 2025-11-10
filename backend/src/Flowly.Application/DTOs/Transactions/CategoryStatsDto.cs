namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Category statistics DTO
/// </summary>
public class CategoryStatsDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}
