namespace Flowly.Application.DTOs.Tasks;

/// <summary>
/// Task recurrence information DTO
/// </summary>
public class RecurrenceDto
{
    public Guid Id { get; set; }
    public string Rule { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastOccurrence { get; set; }
    public DateTime? NextOccurrence { get; set; }
}
