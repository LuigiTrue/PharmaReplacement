using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Repositories.Interfaces;

public interface ILocationRepository
{
    Task<List<Location>> GetAllAsync();
    Task<Location?> GetByIdAsync(int id);
    Task<Location?> GetByCodeAsync(string code);
    Task AddAsync(Location location);
    Task UpdateAsync(Location location);
    Task DeleteAsync(int id);
}
