using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Repositories.Interfaces;

public interface IStockBalanceRepository
{
    Task<List<StockBalance>> GetAllAsync();
    Task<List<StockBalance>> GetByItemIdAsync(int itemId);
    Task<List<StockBalance>> GetByLocationIdAsync(int locationId);
    Task<StockBalance?> GetByItemBatchLocationAsync(int itemId, int batchId, int locationId);
    Task<decimal> GetTotalStockByItemIdAsync(int itemId);
    Task AddAsync(StockBalance stockBalance);
    Task UpdateAsync(StockBalance stockBalance);
    Task DeleteAsync(int id);
}
