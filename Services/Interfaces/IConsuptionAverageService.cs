using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IConsuptionAverageService
{
    //MÉTODOS PRINCIPAIS 
    Task<ConsuptionAverageItem> GetAverageItem();
    Task<ConsuptionAverageItem> GetAverageItemByProductID(int coverageDays);
    Task<List<int>> GetHighAverageItemByVariation (int varitationPercentage);
    Task<List<int>> GetItensLowProjectionDays (int projectionDays);
    Task<List<int>> GetLowProjectionItensByType (int ItemType);
    Task<List<int>> GetItensLowProjetionByOrder (List<ConsuptionAverageItem> item);

}
