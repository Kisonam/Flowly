using System;

namespace Flowly.Domain.Entities;

public class Note
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string? HtmlCache { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();
    public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();

    public ICollection<Link> LinksFrom { get; set; } = new List<Link>();
    public ICollection<Link> LinksTo { get; set; } = new List<Link>();

    // Methods

    public void UpdateContent(string title, string markdown)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(markdown))
            throw new ArgumentException("Markdown cannot be empty", nameof(markdown));

        Title = title.Trim();
        Markdown = markdown;
        HtmlCache = null;
        UpdatedAt = DateTime.UtcNow;
    }
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void CacheHtml(string html)
    {
        HtmlCache = html;
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
    public bool HasValidHtmlCache()
    {
        return !string.IsNullOrEmpty(HtmlCache);
    }
}
