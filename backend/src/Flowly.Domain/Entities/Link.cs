
using Flowly.Domain.Enums;

namespace Flowly.Domain.Entities;
public class Link {
    public Guid Id { get; set; }
    public LinkEntityType FromType { get; set; }
    public Guid FromId { get; set; }
    public LinkEntityType ToType { get; set; }
    public Guid ToId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Methods
 
    public bool IsValid()
    {
        return !(FromType == ToType && FromId == ToId);
    }
    public string GetDescription()
    {
        return $"{FromType} → {ToType}";
    }
    public bool Connects(LinkEntityType type1, Guid id1, LinkEntityType type2, Guid id2)
    {
        return (FromType == type1 && FromId == id1 && ToType == type2 && ToId == id2) ||
               (FromType == type2 && FromId == id2 && ToType == type1 && ToId == id1);
    }
 }