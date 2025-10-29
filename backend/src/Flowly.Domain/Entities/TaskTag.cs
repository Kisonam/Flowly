// backend/src/Flowly.Domain/Entities/TaskTag.cs
namespace Flowly.Domain.Entities;
public class TaskTag { 
    
    public Guid TaskId { get; set; } 
    public Guid TagId { get; set; }
     }