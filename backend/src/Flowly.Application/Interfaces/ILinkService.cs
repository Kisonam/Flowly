using Flowly.Application.DTOs.Links;
using Flowly.Domain.Enums;

namespace Flowly.Application.Interfaces;

public interface ILinkService
{

    Task<LinkDto> CreateLinkAsync(Guid userId, CreateLinkDto dto);

    Task DeleteLinkAsync(Guid userId, Guid linkId);

    Task<List<LinkDto>> GetLinksForEntityAsync(Guid userId, LinkEntityType entityType, Guid entityId);

    Task<EntityPreviewDto> GetPreviewAsync(Guid userId, LinkEntityType entityType, Guid entityId);
}
