using System.Globalization;
using System.Text;
using System.Text.Json;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class ReplacementSettingsService : IReplacementSettingsService
{
    private const int SearchResultLimit = 20;

    private readonly string _minimumStockPath;
    private readonly IMinimumStockService _minimumStockService;
    private readonly IStockJsonService _stockService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ReplacementSettingsService(
        IWebHostEnvironment env,
        IMinimumStockService minimumStockService,
        IStockJsonService stockService)
    {
        _minimumStockPath = Path.Combine(env.ContentRootPath, "storage", "minimos.json");
        _minimumStockService = minimumStockService;
        _stockService = stockService;
    }

    public async Task<List<ReplacementSettingsItem>> SearchPriorityItemsAsync(string searchText)
    {
        var items = await GetConfiguredItemsAsync();
        var normalizedSearch = Normalize(searchText);

        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return items
                .OrderBy(i => i.Name)
                .Take(SearchResultLimit)
                .ToList();
        }

        return items
            .Where(i =>
                Normalize(i.Name).Contains(normalizedSearch) ||
                Normalize(i.Code).Contains(normalizedSearch))
            .OrderBy(i => i.Name)
            .Take(SearchResultLimit)
            .ToList();
    }

    public async Task<ReplacementSettingsItem?> GetPriorityItemAsync(string code)
    {
        var items = await GetConfiguredItemsAsync();
        return items.FirstOrDefault(i => i.Code == code);
    }

    public async Task UpdateItemPriorityAsync(string code, ItemPriority priority)
    {
        var minimums = await _minimumStockService.GetAllAsync();
        var item = minimums.FirstOrDefault(m => m.Code == code);

        if (item is null)
            throw new InvalidOperationException("Item não encontrado nas configurações de estoque mínimo.");

        item.itemPriority = priority;

        var json = JsonSerializer.Serialize(minimums, _jsonOptions);
        await File.WriteAllTextAsync(_minimumStockPath, json);
    }

    private async Task<List<ReplacementSettingsItem>> GetConfiguredItemsAsync()
    {
        var minimums = await _minimumStockService.GetAllAsync();
        var products = await _stockService.GetAllAsync();
        var productsByCode = products
            .GroupBy(p => p.Code)
            .ToDictionary(g => g.Key, g => g.First());

        return minimums
            .Select(minimum =>
            {
                productsByCode.TryGetValue(minimum.Code, out var product);

                return new ReplacementSettingsItem
                {
                    Code = minimum.Code,
                    Name = product?.Name ?? minimum.Name,
                    MinimumQuantity = minimum.MinimumQuantity,
                    ItemPriority = minimum.itemPriority
                };
            })
            .ToList();
    }

    private static string Normalize(string value)
    {
        var normalized = value
            .ToUpperInvariant()
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
