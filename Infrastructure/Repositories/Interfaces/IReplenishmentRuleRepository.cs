using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Repositories.Interfaces;

public interface IReplenishmentRuleRepository
{
    Task<ReplenishmentRule?> GetByItemIdAsync(int itemId);
    Task AddAsync(ReplenishmentRule rule);
    Task UpdateAsync(ReplenishmentRule rule);
    Task DeleteByItemIdAsync(int itemId);
}
