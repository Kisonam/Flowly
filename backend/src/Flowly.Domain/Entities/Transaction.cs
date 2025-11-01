// backend/src/Flowly.Domain/Entities/Transaction.cs

using Flowly.Domain.Enums;

namespace Flowly.Domain.Entities;

/// <summary>
/// Financial transaction - income or expense
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of the transaction
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Transaction amount (always positive, type determines income/expense)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (USD, EUR, UAH, PLN)
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type (Income or Expense)
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Transaction date
    /// </summary>
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the transaction is archived
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When the transaction was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the transaction was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ============================================
    // Navigation Properties
    // ============================================

    /// <summary>
    /// Currency reference
    /// </summary>
    public Currency Currency { get; set; } = null!;

    /// <summary>
    /// Category reference
    /// </summary>
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Links from this transaction to other entities
    /// </summary>
    public ICollection<Link> LinksFrom { get; set; } = new List<Link>();

    /// <summary>
    /// Links to this transaction from other entities
    /// </summary>
    public ICollection<Link> LinksTo { get; set; } = new List<Link>();

    // ============================================
    // Methods
    // ============================================

    /// <summary>
    /// Update transaction details
    /// </summary>
    public void Update(decimal amount, string currencyCode, TransactionType type, Guid categoryId, DateTime date, string? description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Amount = amount;
        CurrencyCode = currencyCode;
        Type = type;
        CategoryId = categoryId;
        Date = date;
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Archive transaction
    /// </summary>
    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restore from archive
    /// </summary>
    public void Restore()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get signed amount (negative for expenses, positive for income)
    /// </summary>
    public decimal GetSignedAmount()
    {
        return Type == TransactionType.Expense ? -Amount : Amount;
    }
}