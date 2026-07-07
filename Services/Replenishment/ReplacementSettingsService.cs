using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Replenishment;

public class ReplacementSettingsService : IReplacementSettingsService
{
    private const int SearchResultLimit = 20;

    private readonly IMinimumStockService _minimumStockService;
    private readonly IStockJsonService _stockService;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ReplacementSettingsService(
        IMinimumStockService minimumStockService,
        IStockJsonService stockService,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _minimumStockService = minimumStockService;
        _stockService = stockService;
        _dbContextFactory = dbContextFactory;
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
        return items.FirstOrDefault(i =>
            string.Equals(i.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task UpdateItemSettingsAsync(string code, ItemPriority priority, decimal minimumQuantity, ItemType itemType)
    {
        if (minimumQuantity < 0)
            throw new InvalidOperationException("O estoque mínimo não pode ser negativo.");

        var minimums = await _minimumStockService.GetAllAsync();
        var normalizedCode = code.Trim();
        var item = minimums.FirstOrDefault(m =>
            string.Equals(m.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));

        if (item is null)
        {
            var product = await _stockService.GetByCodeAsync(normalizedCode);
            if (product is null)
                throw new InvalidOperationException("Item não encontrado no estoque.");

            item = new MinimumStock
            {
                Code = product.Code,
                Name = product.Name,
                MinimumQuantity = 0,
                itemPriority = ItemPriority.Low
            };
        }

        await _minimumStockService.SaveAsync(new MinimumStock
        {
            Code = item.Code,
            Name = item.Name,
            MinimumQuantity = minimumQuantity,
            itemPriority = priority
        });

        await UpdateItemTypeAsync(normalizedCode, itemType);
    }

    public async Task AddMinimumStockItemAsync(string code, string name, ItemPriority priority, decimal minimumQuantity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Informe o código do item.");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Informe o nome do item.");

        if (minimumQuantity < 0)
            throw new InvalidOperationException("O estoque mínimo não pode ser negativo.");

        var normalizedCode = code.Trim();
        var minimums = await _minimumStockService.GetAllAsync();

        if (minimums.Any(m => string.Equals(m.Code, normalizedCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Este item já possui estoque mínimo cadastrado.");

        var product = await _stockService.GetByCodeAsync(normalizedCode);

        await _minimumStockService.SaveAsync(new MinimumStock
        {
            Code = normalizedCode,
            Name = product?.Name ?? name.Trim(),
            MinimumQuantity = minimumQuantity,
            itemPriority = priority
        });
    }

    private async Task<List<ReplacementSettingsItem>> GetConfiguredItemsAsync()
    {
        var minimums = await _minimumStockService.GetAllAsync();
        var products = await _stockService.GetAllAsync();

        var minimumsByCode = minimums
            .GroupBy(m => m.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var productsByCode = products
            .GroupBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = products
            .Select(product =>
            {
                minimumsByCode.TryGetValue(product.Code, out var minimum);

                return new ReplacementSettingsItem
                {
                    Code = product.Code,
                    Name = product.Name,
                    MinimumQuantity = minimum?.MinimumQuantity ?? 0,
                    ItemPriority = minimum?.itemPriority ?? ItemPriority.Low,
                    ItemType = product.ItemType
                };
            })
            .ToList();

        foreach (var minimum in minimums)
        {
            if (productsByCode.ContainsKey(minimum.Code))
                continue;

            result.Add(new ReplacementSettingsItem
            {
                Code = minimum.Code,
                Name = minimum.Name,
                MinimumQuantity = minimum.MinimumQuantity,
                ItemPriority = minimum.itemPriority,
                ItemType = ItemType.CommonMedication
            });
        }

        return result;
    }

    private async Task UpdateItemTypeAsync(string code, ItemType itemType)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var item = await context.Items.FirstOrDefaultAsync(existingItem => existingItem.Code == code);

        if (item is null)
            return;

        item.ItemType = itemType;
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
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
