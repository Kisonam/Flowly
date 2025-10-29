// backend/src/Flowly.Domain/Entities/Category.cs
namespace Flowly.Domain.Entities;
public class Category 
{ 
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}