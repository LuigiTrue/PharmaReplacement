using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IMinimumStockService
{
    Task<List<MinimumStock>> GetAllAsync();
    Task<MinimumStock?> GetByCodeAsync(string code);
    Task SaveAsync(MinimumStock item);
    Task RemoveAsync(string code);
    Task<List<ProductStock>> GetProductsWithoutMinimumAsync();

}
