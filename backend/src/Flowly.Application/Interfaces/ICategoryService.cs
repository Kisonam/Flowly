using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.Interfaces;

/// <summary>
/// Service for managing finance categories
/// </summary>
public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(Guid userId);
    Task<CategoryDto> GetByIdAsync(Guid userId, Guid categoryId);
    Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(Guid userId, Guid categoryId, UpdateCategoryDto dto);
    Task DeleteAsync(Guid userId, Guid categoryId);
}
