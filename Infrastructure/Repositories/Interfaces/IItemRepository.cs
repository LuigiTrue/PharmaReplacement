using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Repositories.Interfaces;

public interface IItemRepository
{
    Task<List<Item>> GetAllAsync();
    Task<Item?> GetByIdAsync(int id);
    Task<Item?> GetByCodeAsync(string code);
    Task<List<Item>> SearchByNameAsync(string searchTerm);
    Task AddAsync(Item item);
    Task UpdateAsync(Item item);
    Task DeleteAsync(int id);
    Task<bool> ExistsByCodeAsync(string code);
}
