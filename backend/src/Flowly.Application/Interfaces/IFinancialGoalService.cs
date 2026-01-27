using Flowly.Application.DTOs.Common; 
using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.Interfaces;

public interface IFinancialGoalService
{
    Task<List<FinancialGoalDto>> GetAllAsync(Guid userId, GoalFilterDto? filter = null);
    Task<FinancialGoalDto> GetByIdAsync(Guid userId, Guid goalId);
    Task<FinancialGoalDto> CreateAsync(Guid userId, CreateGoalDto dto);
    Task<FinancialGoalDto> UpdateAsync(Guid userId, Guid goalId, UpdateGoalDto dto);
    Task DeleteAsync(Guid userId, Guid goalId);
    Task ArchiveAsync(Guid userId, Guid goalId);
    Task RestoreAsync(Guid userId, Guid goalId);

    Task<FinancialGoalDto> AddAmountAsync(Guid userId, Guid goalId, UpdateGoalAmountDto dto);
    Task<FinancialGoalDto> SubtractAmountAsync(Guid userId, Guid goalId, UpdateGoalAmountDto dto);
    Task<FinancialGoalDto> SetCurrentAmountAsync(Guid userId, Guid goalId, UpdateGoalAmountDto dto);

    Task<List<TransactionListItemDto>> GetGoalTransactionsAsync(Guid userId, Guid goalId);
}
