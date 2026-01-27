using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.Interfaces;

public interface IBudgetService
{
    Task<List<BudgetDto>> GetAllAsync(Guid userId, BudgetFilterDto? filter = null);
    Task<BudgetDto> GetByIdAsync(Guid userId, Guid budgetId);
    Task<BudgetDto> CreateAsync(Guid userId, CreateBudgetDto dto);
    Task<BudgetDto> UpdateAsync(Guid userId, Guid budgetId, UpdateBudgetDto dto);
    Task DeleteAsync(Guid userId, Guid budgetId);
    Task ArchiveAsync(Guid userId, Guid budgetId);
    Task RestoreAsync(Guid userId, Guid budgetId);

    Task<bool> IsOverspentAsync(Guid userId, Guid budgetId);

    Task<List<TransactionListItemDto>> GetBudgetTransactionsAsync(Guid userId, Guid budgetId);
}
