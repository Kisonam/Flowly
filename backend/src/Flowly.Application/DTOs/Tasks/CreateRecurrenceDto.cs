namespace Flowly.Application.DTOs.Tasks;

/// <summary>
/// DTO for creating task recurrence
/// </summary>
public class CreateRecurrenceDto
{
    public string Rule { get; set; } = string.Empty;
}
