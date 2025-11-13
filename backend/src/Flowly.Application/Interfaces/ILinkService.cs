using Flowly.Application.DTOs.Links;
using Flowly.Domain.Enums;

namespace Flowly.Application.Interfaces;

public interface ILinkService
{
    /// <summary>
    /// Create a new link between two entities
    /// Validates that both entities belong to the user
    /// </summary>
    Task<LinkDto> CreateLinkAsync(Guid userId, CreateLinkDto dto);

    /// <summary>
    /// Delete a link by ID
    /// </summary>
    Task DeleteLinkAsync(Guid userId, Guid linkId);

    /// <summary>
    /// Get all links for a specific entity
    /// Returns links where the entity is either source or target
    /// </summary>
    Task<List<LinkDto>> GetLinksForEntityAsync(Guid userId, LinkEntityType entityType, Guid entityId);

    /// <summary>
    /// Get a preview of an entity (for displaying in link previews)
    /// </summary>
    Task<EntityPreviewDto> GetPreviewAsync(Guid userId, LinkEntityType entityType, Guid entityId);
}
