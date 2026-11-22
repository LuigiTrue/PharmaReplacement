using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Import;

public class StockJsonService : IStockJsonService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public StockJsonService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<ProductStock>> GetAllAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var items = await context.Items
            .AsNoTracking()
            .Where(item => item.IsActive)
            .Include(item => item.StockBalances)
                .ThenInclude(balance => balance.Batch)
            .Include(item => item.StockBalances)
                .ThenInclude(balance => balance.Location)
            .OrderBy(item => item.Name)
            .ToListAsync();

        return items
            .Select(item => new ProductStock
            {
                Code = item.Code,
                Name = item.Name,
                Unit = item.Unit,
                ItemType = item.ItemType,
                TotalStock = item.StockBalances.Sum(balance => balance.Quantity),
                Batches = item.StockBalances
                    .GroupBy(balance => new
                    {
                        balance.BatchId,
                        balance.Batch.BatchNumber,
                        balance.Batch.Validity
                    })
                    .OrderBy(group => group.Key.Validity)
                    .Select(group => new BatchStock
                    {
                        Batch = group.Key.BatchNumber,
                        Validity = group.Key.Validity,
                        Locations = group
                            .OrderBy(balance => balance.Location.Code)
                            .Select(balance => new StockLocation
                            {
                                LocationId = balance.Location.Code,
                                Quantity = balance.Quantity
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();
    }
}
