using Flowly.Application.DTOs.Notes;
using Flowly.Domain.Enums;

namespace Flowly.Application.DTOs.Transactions;

/// <summary>
/// Complete transaction information DTO
/// </summary>
public class TransactionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Related entities
    public CategoryDto? Category { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}
