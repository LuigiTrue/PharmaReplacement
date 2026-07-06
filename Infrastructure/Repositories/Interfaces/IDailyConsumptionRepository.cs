using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Repositories.Interfaces;

public interface IDailyConsumptionRepository
{
    Task<List<DailyConsumption>> GetByItemIdAsync(int itemId);
    Task<List<DailyConsumption>> GetByItemIdAndPeriodAsync(int itemId, DateTime startDate, DateTime endDate);
    Task<decimal> GetAverageDailyConsumptionAsync(int itemId, DateTime startDate, DateTime endDate);
    Task AddAsync(DailyConsumption dailyConsumption);
    Task AddRangeAsync(IEnumerable<DailyConsumption> dailyConsumptions);
    Task DeleteByItemIdAndPeriodAsync(int itemId, DateTime startDate, DateTime endDate);
}
