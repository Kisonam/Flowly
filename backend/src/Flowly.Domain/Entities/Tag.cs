namespace Flowly.Domain.Entities;
public class Tag
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
    public ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        Name = name.Trim().ToLowerInvariant();
    }

    public void UpdateColor(string? color)
    {
        if (!string.IsNullOrEmpty(color) && !IsValidHexColor(color))
            throw new ArgumentException("Invalid hex color format", nameof(color));

        Color = color;
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color)) return false;
        if (!color.StartsWith("#")) return false;
        
        var hex = color.Substring(1);
        return (hex.Length == 6 || hex.Length == 3) && 
               hex.All(c => "0123456789ABCDEFabcdef".Contains(c));
    }
}