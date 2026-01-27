namespace Flowly.Domain.Entities;
public class MediaAsset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? NoteId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Note? Note { get; set; }

    public void AttachToNote(Guid noteId)
    {
        NoteId = noteId;
    }

    public void Detach()
    {
        NoteId = null;
    }

    public bool IsImage()
    {
        return MimeType.StartsWith("image/");
    }

    public string GetExtension()
    {
        return System.IO.Path.GetExtension(FileName);
    }

    public string GetFormattedSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = Size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
