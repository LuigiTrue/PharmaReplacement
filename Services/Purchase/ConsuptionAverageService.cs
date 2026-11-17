using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Purchase;

public class ConsumptionAverageReportService : IConsuptionAverageService
{
    public Task<ConsuptionAverageItem> GetAverageItem()
    {
        throw new NotImplementedException();
    }

    public Task<ConsuptionAverageItem> GetAverageItemByProductID(int coverageDays)
    {
        throw new NotImplementedException();
    }

    public Task<List<int>> GetHighAverageItemByVariation(int varitationPercentage)
    {
        throw new NotImplementedException();
    }

    public Task<List<int>> GetItensLowProjectionDays(int projectionDays)
    {
        throw new NotImplementedException();
    }

    public Task<List<int>> GetLowProjectionItensByType(int ItemType)
    {
        throw new NotImplementedException();
    }

    public Task<List<int>> GetItensLowProjetionByOrder(List<ConsuptionAverageItem> item)
    {
        throw new NotImplementedException();
    }
}
