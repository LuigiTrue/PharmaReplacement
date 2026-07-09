using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IDailyReplenishmentService
{
    Task<DailyReplenishmentData> GetDailyReplenishmentAsync(int coverageDays);
}
