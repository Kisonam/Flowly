using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.Interfaces;

public interface ITransactionService
{

    Task<PagedResult<TransactionListItemDto>> GetAllAsync(Guid userId, TransactionFilterDto filter);

    Task<TransactionDto> GetByIdAsync(Guid userId, Guid transactionId);

    Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionDto dto);

    Task<TransactionDto> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionDto dto);

    Task ArchiveAsync(Guid userId, Guid transactionId);

    Task RestoreAsync(Guid userId, Guid transactionId);

    Task<FinanceStatsDto> GetStatsAsync(Guid userId, DateTime periodStart, DateTime periodEnd, string? currencyCode = null);
}
