using RepyPharma.Models;
using System.Text.Json;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Inventory;

public class MinimumStockService : IMinimumStockService
{
    private readonly string _filePath;
    private readonly IStockJsonService _stockService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MinimumStockService(IWebHostEnvironment env, IStockJsonService stockService)
    {
        _filePath = Path.Combine(env.ContentRootPath, "storage", "minimos.json");
        _stockService = stockService;
    }

    // Retorna todos os mínimos cadastrados
    public async Task<List<MinimumStock>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<MinimumStock>();

        var json = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(json))
            return new List<MinimumStock>();

        return JsonSerializer.Deserialize<List<MinimumStock>>(json, _jsonOptions)
               ?? new List<MinimumStock>();
    }

    // Retorna o mínimo de um produto específico
    public async Task<MinimumStock?> GetByCodeAsync(string code)
    {
        var minimos = await GetAllAsync();
        return minimos.FirstOrDefault(m => m.Code == code);
    }

    // Salva ou atualiza o mínimo de um produto
    public async Task SaveAsync(MinimumStock item)
    {
        var minimos = await GetAllAsync();
        var existing = minimos.FirstOrDefault(m => m.Code == item.Code);

        if (existing is not null)
        {
            existing.MinimumQuantity = item.MinimumQuantity;
        }
        else
        {
            // Garante que o nome vem do estoque, não digitado manualmente
            var product = await _stockService.GetByCodeAsync(item.Code);
            item.Name = product?.Name ?? item.Name;
            minimos.Add(item);
        }

        await WriteAsync(minimos);
    }

    // Remove o mínimo de um produto
    public async Task RemoveAsync(string code)
    {
        var minimos = await GetAllAsync();
        var item = minimos.FirstOrDefault(m => m.Code == code);

        if (item is not null)
        {
            minimos.Remove(item);
            await WriteAsync(minimos);
        }
    }

    // Retorna apenas produtos que ainda não têm mínimo cadastrado
    public async Task<List<ProductStock>> GetProductsWithoutMinimumAsync()
    {
        var products = await _stockService.GetAllAsync();
        var minimos = await GetAllAsync();
        var codesComMinimo = minimos.Select(m => m.Code).ToHashSet();

        return products.Where(p => !codesComMinimo.Contains(p.Code)).ToList();
    }

    private async Task WriteAsync(List<MinimumStock> minimos)
    {
        var json = JsonSerializer.Serialize(minimos, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
