// backend/src/Flowly.Application/DTOs/Export/ExportDataDto.cs

namespace Flowly.Application.DTOs.Export;

public class ExportDataDto
{
    public UserExportDto User { get; set; } = null!;
    public List<NoteExportDto> Notes { get; set; } = new();
    public List<TaskExportDto> Tasks { get; set; } = new();
    public List<TransactionExportDto> Transactions { get; set; } = new();
    public List<BudgetExportDto> Budgets { get; set; } = new();
    public List<GoalExportDto> Goals { get; set; } = new();
    public List<CategoryExportDto> Categories { get; set; } = new();
    public List<TagExportDto> Tags { get; set; } = new();
    public DateTime ExportedAt { get; set; }
}

public class UserExportDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class NoteExportDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TaskExportDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? Theme { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TransactionExportDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BudgetExportDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class GoalExportDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CategoryExportDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class TagExportDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
