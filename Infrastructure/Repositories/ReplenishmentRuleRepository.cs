using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Repositories.Interfaces;

namespace RepyPharma.Infrastructure.Repositories;

public class ReplenishmentRuleRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IReplenishmentRuleRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<ReplenishmentRule?> GetByItemIdAsync(int itemId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.ReplenishmentRules
            .AsNoTracking()
            .FirstOrDefaultAsync(rule => rule.ItemId == itemId);
    }

    public async Task AddAsync(ReplenishmentRule rule)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await context.ReplenishmentRules.AddAsync(rule);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ReplenishmentRule rule)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        context.ReplenishmentRules.Update(rule);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByItemIdAsync(int itemId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var rule = await context.ReplenishmentRules
            .FirstOrDefaultAsync(replenishmentRule => replenishmentRule.ItemId == itemId);

        if (rule is null)
            return;

        context.ReplenishmentRules.Remove(rule);
        await context.SaveChangesAsync();
    }
}
