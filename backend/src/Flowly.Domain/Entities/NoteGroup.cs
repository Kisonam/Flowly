namespace Flowly.Domain.Entities;

public class NoteGroup
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Note> Notes { get; set; } = new List<Note>();

    // Methods
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Group title cannot be empty", nameof(title));
        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateColor(string? color)
    {
        Color = color;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateOrder(int order)
    {
        if (order < 0)
            throw new ArgumentException("Order cannot be negative", nameof(order));
        Order = order;
        UpdatedAt = DateTime.UtcNow;
    }
}
