using RepyPharma.Models;
using System.Text.Json;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Import;

public class StockJsonService : IStockJsonService
{
    private readonly string _filePath;
    private readonly string _ignoredFilePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StockJsonService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "storage", "estoque.json");
        _ignoredFilePath = Path.Combine(env.ContentRootPath, "storage", "ignorados.json");

    }

    public async Task<List<ProductStock>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<ProductStock>();

        var json = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new List<ProductStock>();

        return JsonSerializer.Deserialize<List<ProductStock>>(json, _jsonOptions)
               ?? new List<ProductStock>();
    }

    public async Task<ProductStock?> GetByCodeAsync(string code)
    {
        var products = await GetAllAsync();
        return products.FirstOrDefault(p => p.Code == code);
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
