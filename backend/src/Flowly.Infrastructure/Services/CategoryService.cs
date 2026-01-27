using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowly.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CategoryDto>> GetAllAsync(Guid userId)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId || c.UserId == null) 
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                UserId = c.UserId,
                Color = c.Color,
                Icon = c.Icon
            })
            .ToListAsync();

        return categories;
    }

    public async Task<CategoryDto> GetByIdAsync(Guid userId, Guid categoryId)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId 
                && (c.UserId == userId || c.UserId == null));

        if (category == null)
        {
            throw new InvalidOperationException("Category not found");
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            UserId = category.UserId,
            Color = category.Color,
            Icon = category.Icon
        };
    }

    public async Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Category name is required", nameof(dto.Name));
        }

        var exists = await _dbContext.Categories
            .AnyAsync(c => c.UserId == userId && c.Name == dto.Name.Trim());

        if (exists)
        {
            throw new InvalidOperationException("Category with this name already exists");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Color = dto.Color?.Trim(),
            Icon = dto.Icon?.Trim()
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            UserId = category.UserId,
            Color = category.Color,
            Icon = category.Icon
        };
    }

    public async Task<CategoryDto> UpdateAsync(Guid userId, Guid categoryId, UpdateCategoryDto dto)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null)
        {
            throw new InvalidOperationException("Category not found or cannot be modified");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Category name is required", nameof(dto.Name));
        }

        var exists = await _dbContext.Categories
            .AnyAsync(c => c.UserId == userId 
                && c.Name == dto.Name.Trim() 
                && c.Id != categoryId);

        if (exists)
        {
            throw new InvalidOperationException("Category with this name already exists");
        }

        category.Name = dto.Name.Trim();
        category.Color = dto.Color?.Trim();
        category.Icon = dto.Icon?.Trim();
        await _dbContext.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            UserId = category.UserId,
            Color = category.Color,
            Icon = category.Icon
        };
    }

    public async Task DeleteAsync(Guid userId, Guid categoryId)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId);

        if (category == null)
        {
            throw new InvalidOperationException("Category not found or cannot be deleted");
        }

        var hasTransactions = await _dbContext.Transactions
            .AnyAsync(t => t.CategoryId == categoryId);

        if (hasTransactions)
        {
            throw new InvalidOperationException("Cannot delete category that has associated transactions");
        }

        var hasBudgets = await _dbContext.Budgets
            .AnyAsync(b => b.CategoryId == categoryId);

        if (hasBudgets)
        {
            throw new InvalidOperationException("Cannot delete category that has associated budgets");
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync();
    }
}
