using RepyPharma.Models;
using System.Text.Json;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class StockJsonService : IStockJsonService
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StockJsonService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "storage", "estoque.json");
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
}
