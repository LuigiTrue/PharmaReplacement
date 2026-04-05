using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IReplenishmentService
{
    Task<List<ReplenishmentItem>> GenerateAsync();
    Task<DashboardSummary> GetDashboardSummaryAsync();
}