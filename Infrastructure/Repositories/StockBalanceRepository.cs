using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Repositories.Interfaces;

namespace RepyPharma.Infrastructure.Repositories;

public class StockBalanceRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IStockBalanceRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<List<StockBalance>> GetAllAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await GetStockBalancesWithIncludes(context)
            .ToListAsync();
    }

    public async Task<List<StockBalance>> GetByItemIdAsync(int itemId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await GetStockBalancesWithIncludes(context)
            .Where(balance => balance.ItemId == itemId)
            .ToListAsync();
    }

    public async Task<List<StockBalance>> GetByLocationIdAsync(int locationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await GetStockBalancesWithIncludes(context)
            .Where(balance => balance.LocationId == locationId)
            .ToListAsync();
    }

    public async Task<StockBalance?> GetByItemBatchLocationAsync(int itemId, int batchId, int locationId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await GetStockBalancesWithIncludes(context)
            .FirstOrDefaultAsync(balance =>
                balance.ItemId == itemId &&
                balance.BatchId == batchId &&
                balance.LocationId == locationId);
    }

    public async Task<decimal> GetTotalStockByItemIdAsync(int itemId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.StockBalances
            .AsNoTracking()
            .Where(balance => balance.ItemId == itemId)
            .SumAsync(balance => balance.Quantity);
    }

    public async Task AddAsync(StockBalance stockBalance)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await context.StockBalances.AddAsync(stockBalance);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(StockBalance stockBalance)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        context.StockBalances.Update(stockBalance);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var stockBalance = await context.StockBalances.FindAsync(id);
        if (stockBalance is null)
            return;

        context.StockBalances.Remove(stockBalance);
        await context.SaveChangesAsync();
    }

    private static IQueryable<StockBalance> GetStockBalancesWithIncludes(AppDbContext context)
    {
        return context.StockBalances
            .AsNoTracking()
            .Include(balance => balance.Item)
            .Include(balance => balance.Batch)
            .Include(balance => balance.Location);
    }
}
