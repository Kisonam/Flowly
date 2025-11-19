using Flowly.Application.DTOs.Dashboard;

namespace Flowly.Application.Interfaces;

/// <summary>
/// Service for dashboard overview data
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get dashboard overview with finance stats, upcoming tasks, and recent notes
    /// Optimized to minimize database calls
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Dashboard data</returns>
    Task<DashboardDto> GetDashboardAsync(Guid userId);
}
