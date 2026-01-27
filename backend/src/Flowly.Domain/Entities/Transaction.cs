using Flowly.Domain.Enums;
namespace Flowly.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BudgetId { get; set; }
    public Guid? GoalId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Currency Currency { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Budget? Budget { get; set; }
    public FinancialGoal? Goal { get; set; }
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
    public ICollection<Link> LinksFrom { get; set; } = new List<Link>();
    public ICollection<Link> LinksTo { get; set; } = new List<Link>();

    public void Update(string title, decimal amount, string currencyCode, 
    TransactionType type, Guid? categoryId, DateTime date, 
    string? description = null, Guid? budgetId = null, Guid? goalId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Title = title.Trim();
        Amount = amount;
        CurrencyCode = currencyCode;
        Type = type;
        CategoryId = categoryId;
        Date = date;
        Description = description?.Trim();
        BudgetId = budgetId;
        GoalId = goalId;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Restore()
    {
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }
    public decimal GetSignedAmount()
    {
        return Type == TransactionType.Expense ? -Amount : Amount;
    }
}