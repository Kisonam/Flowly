using System;

namespace Flowly.Domain.Entities;

public class Note
{
     /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Owner of the note
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Note title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Markdown content (unlimited length)
    /// </summary>
    public string Markdown { get; set; } = string.Empty;

    /// <summary>
    /// Cached HTML rendered from Markdown (for performance)
    /// Optional - can be null and regenerated on demand
    /// </summary>
    public string? HtmlCache { get; set; }

    /// <summary>
    /// Whether the note is archived (soft delete)
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// When the note was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the note was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ============================================
    // Navigation Properties
    // ============================================

    /// <summary>
    /// Note owner (User)
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Tags associated with this note (many-to-many through NoteTag)
    /// </summary>
    public ICollection<NoteTag> NoteTags { get; set; } = new List<NoteTag>();

    /// <summary>
    /// Media assets (images, files) attached to this note
    /// </summary>
    public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();

    /// <summary>
    /// Links from this note to other entities (tasks, transactions, other notes)
    /// </summary>
    public ICollection<Link> LinksFrom { get; set; } = new List<Link>();

    /// <summary>
    /// Links to this note from other entities
    /// </summary>
    public ICollection<Link> LinksTo { get; set; } = new List<Link>();

    // ============================================
    // Methods
    // ============================================

    /// <summary>
    /// Update note content and invalidate HTML cache
    /// </summary>
    public void UpdateContent(string title, string markdown)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(markdown))
            throw new ArgumentException("Markdown cannot be empty", nameof(markdown));

        Title = title.Trim();
        Markdown = markdown;
        HtmlCache = null; // Invalidate cache - will be regenerated
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update only the title
    /// </summary>
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cache the rendered HTML
    /// </summary>
    public void CacheHtml(string html)
    {
        HtmlCache = html;
    }

    /// <summary>
    /// Archive the note (soft delete)
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
    /// Check if HTML cache is valid (exists and markdown hasn't changed)
    /// This is a simple check - in real app you might want to hash markdown
    /// </summary>
    public bool HasValidHtmlCache()
    {
        return !string.IsNullOrEmpty(HtmlCache);
    }
}
