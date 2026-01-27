using Flowly.Application.DTOs.Dashboard;

namespace Flowly.Application.Interfaces;

public interface IDashboardService
{

    Task<DashboardDto> GetDashboardAsync(Guid userId);
}
