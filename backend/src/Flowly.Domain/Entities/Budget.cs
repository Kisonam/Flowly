

namespace Flowly.Domain.Entities;

public class Budget
{

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal Limit { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsArchived { get; set; } = false;

    public DateTime? ArchivedAt { get; set; }

    public Currency Currency { get; set; } = null!;

    public Category? Category { get; set; }

    public void Update(string title, string? description, DateTime periodStart, DateTime periodEnd, decimal limit, string currencyCode, Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (periodEnd <= periodStart)
            throw new ArgumentException("Period end must be after period start");

        if (limit <= 0)
            throw new ArgumentException("Limit must be positive", nameof(limit));

        Title = title;
        Description = description;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Limit = limit;
        CurrencyCode = currencyCode;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive()
    {
        var now = DateTime.UtcNow;
        return now >= PeriodStart && now <= PeriodEnd;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > PeriodEnd;
    }

    public int GetDaysRemaining()
    {
        return (PeriodEnd - DateTime.UtcNow).Days;
    }

    public void Archive()
    {
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsArchived = false;
        ArchivedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}