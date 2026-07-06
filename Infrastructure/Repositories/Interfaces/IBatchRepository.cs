using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Repositories.Interfaces;

public interface IBatchRepository
{
    Task<List<Batch>> GetByItemIdAsync(int itemId);
    Task<Batch?> GetByIdAsync(int id);
    Task<Batch?> GetByItemAndBatchNumberAsync(int itemId, string batchNumber);
    Task AddAsync(Batch batch);
    Task UpdateAsync(Batch batch);
    Task DeleteAsync(int id);
}
