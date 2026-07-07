using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Models;
using System.Text.Json;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Import;

public class StockJsonService : IStockJsonService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly string _ignoredFilePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StockJsonService(IDbContextFactory<AppDbContext> dbContextFactory, IWebHostEnvironment env)
    {
        _dbContextFactory = dbContextFactory;
        _ignoredFilePath = Path.Combine(env.ContentRootPath, "storage", "ignorados.json");

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

    public async Task<ProductStock?> GetByCodeAsync(string code)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var item = await context.Items
            .AsNoTracking()
            .Where(item => item.IsActive && item.Code == code)
            .Include(item => item.StockBalances)
                .ThenInclude(balance => balance.Batch)
            .Include(item => item.StockBalances)
                .ThenInclude(balance => balance.Location)
            .FirstOrDefaultAsync();

        if (item is null)
            return null;

        return new ProductStock
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
        };
    }

    private static readonly Dictionary<string, string> LocationNames = new()
{
    { "996", "Almoxarifado" },
    { "997", "Farmácia Central" },
    { "998", "Farmácia Centro Cirúrgico" },
    { "999", "CAF" },
    { "1059", "Fracionamento" }
};

    public async Task<List<LocationSummary>> GetLocationSummaryAsync()
    {
        var products = await GetAllAsync();

        var totals = LocationNames.ToDictionary(
            kv => kv.Key,
            kv => new LocationSummary
            {
                LocationId = kv.Key,
                LocationName = kv.Value,
                TotalQuantity = 0
            });

        foreach (var product in products)
            foreach (var batch in product.Batches)
                foreach (var location in batch.Locations)
                    if (totals.TryGetValue(location.LocationId, out var summary))
                        summary.TotalQuantity += location.Quantity;

        return totals.Values
            .Where(l => l.TotalQuantity > 0)
            .OrderByDescending(l => l.TotalQuantity)
            .ToList();
    }

    public async Task<HashSet<string>> GetIgnoredCodesAsync()
    {
        if (!File.Exists(_ignoredFilePath))
            return new HashSet<string>();

        var json = await File.ReadAllTextAsync(_ignoredFilePath);

        if (string.IsNullOrWhiteSpace(json))
            return new HashSet<string>();

        var codes = JsonSerializer.Deserialize<List<string>>(json, _jsonOptions);
        return codes?.ToHashSet() ?? new HashSet<string>();
    }
}
