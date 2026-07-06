using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Infrastructure.Repositories.Interfaces;

namespace RepyPharma.Infrastructure.Repositories;

public class DailyConsumptionRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IDailyConsumptionRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<List<DailyConsumption>> GetByItemIdAsync(int itemId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.DailyConsumptions
            .AsNoTracking()
            .Where(consumption => consumption.ItemId == itemId)
            .OrderBy(consumption => consumption.ConsumptionDate)
            .ToListAsync();
    }

    public async Task<List<DailyConsumption>> GetByItemIdAndPeriodAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.DailyConsumptions
            .AsNoTracking()
            .Where(consumption =>
                consumption.ItemId == itemId &&
                consumption.ConsumptionDate >= startDate &&
                consumption.ConsumptionDate <= endDate)
            .OrderBy(consumption => consumption.ConsumptionDate)
            .ToListAsync();
    }

    public async Task<decimal> GetAverageDailyConsumptionAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.DailyConsumptions
            .AsNoTracking()
            .Where(consumption =>
                consumption.ItemId == itemId &&
                consumption.ConsumptionDate >= startDate &&
                consumption.ConsumptionDate <= endDate)
            .Select(consumption => (decimal?)consumption.Quantity)
            .AverageAsync() ?? 0m;
    }

    public async Task AddAsync(DailyConsumption dailyConsumption)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await context.DailyConsumptions.AddAsync(dailyConsumption);
        await context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<DailyConsumption> dailyConsumptions)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await context.DailyConsumptions.AddRangeAsync(dailyConsumptions);
        await context.SaveChangesAsync();
    }

    public async Task DeleteByItemIdAndPeriodAsync(int itemId, DateTime startDate, DateTime endDate)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var consumptions = await context.DailyConsumptions
            .Where(consumption =>
                consumption.ItemId == itemId &&
                consumption.ConsumptionDate >= startDate &&
                consumption.ConsumptionDate <= endDate)
            .ToListAsync();

        if (consumptions.Count == 0)
            return;

        context.DailyConsumptions.RemoveRange(consumptions);
        await context.SaveChangesAsync();
    }
}
