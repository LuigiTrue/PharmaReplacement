using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IReplenishmentDashboardService
{
    Task<ReplenishmentDashboardData> GetDashboardDataAsync();
}
