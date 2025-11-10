using Flowly.Application.DTOs.Common;
using Flowly.Application.DTOs.Transactions;

namespace Flowly.Application.Interfaces;

/// <summary>
/// Service for managing financial transactions and related analytics
/// </summary>
public interface ITransactionService
{
    // ============================================
    // CRUD & Query
    // ============================================

    /// <summary>
    /// Get transactions for a user with filtering and pagination
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="filter">Filter including pagination</param>
    /// <returns>Paged list of transactions (lightweight list items)</returns>
    Task<PagedResult<TransactionListItemDto>> GetAllAsync(Guid userId, TransactionFilterDto filter);

    /// <summary>
    /// Get single transaction by id
    /// </summary>
    Task<TransactionDto> GetByIdAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Create a new transaction
    /// </summary>
    Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionDto dto);

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    Task<TransactionDto> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionDto dto);

    /// <summary>
    /// Archive transaction (soft delete)
    /// </summary>
    Task ArchiveAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Restore archived transaction
    /// </summary>
    Task RestoreAsync(Guid userId, Guid transactionId);

    // ============================================
    // Stats
    // ============================================

    /// <summary>
    /// Get aggregated finance statistics for a period
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="periodStart">Start date (inclusive)</param>
    /// <param name="periodEnd">End date (inclusive)</param>
    /// <param name="currencyCode">Optional currency for reporting</param>
    Task<FinanceStatsDto> GetStatsAsync(Guid userId, DateTime periodStart, DateTime periodEnd, string? currencyCode = null);
}
