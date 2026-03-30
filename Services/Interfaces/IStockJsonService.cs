using RepyPharma.Models;

namespace RepyPharma.Services.Interfaces;

public interface IStockJsonService
{
    Task<List<ProductStock>> GetAllAsync();
    Task<ProductStock?> GetByCodeAsync(string code);
}
