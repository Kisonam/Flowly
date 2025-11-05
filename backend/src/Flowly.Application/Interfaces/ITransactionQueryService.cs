using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.Interfaces;

public interface ITransactionQueryService
{
    Task<List<TransactionListItemDto>> GetListAsync(Guid userId, string? search = null, bool? isArchived = null, int take = 50);
}
