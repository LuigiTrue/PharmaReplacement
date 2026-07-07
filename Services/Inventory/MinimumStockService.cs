using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain;
using RepyPharma.Domain.Entities;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;
using System.Text.Json;

namespace RepyPharma.Services.Inventory;

public class MinimumStockService : IMinimumStockService
{
    private const string ManualCalculationMethod = "manual";

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly string _legacyFilePath;
    private readonly ReplenishmentDataState _replenishmentDataState;
    private readonly IStockJsonService _stockService;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MinimumStockService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IWebHostEnvironment env,
        ReplenishmentDataState replenishmentDataState,
        IStockJsonService stockService)
    {
        _dbContextFactory = dbContextFactory;
        _legacyFilePath = Path.Combine(env.ContentRootPath, "storage", "minimos.json");
        _replenishmentDataState = replenishmentDataState;
        _stockService = stockService;
    }

    public async Task<List<MinimumStock>> GetAllAsync()
    {
        await EnsureLegacyMinimumsImportedAsync();

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.ReplenishmentRules
            .AsNoTracking()
            .Where(rule => rule.IsActive)
            .Include(rule => rule.Item)
            .OrderBy(rule => rule.Item.Name)
            .Select(rule => new MinimumStock
            {
                Code = rule.Item.Code,
                Name = rule.Item.Name,
                MinimumQuantity = rule.MinimumStock ?? 0,
                itemPriority = rule.ItemPriority
            })
            .ToListAsync();
    }

    public async Task<MinimumStock?> GetByCodeAsync(string code)
    {
        await EnsureLegacyMinimumsImportedAsync();

        var normalizedCode = code.Trim();
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.ReplenishmentRules
            .AsNoTracking()
            .Where(rule => rule.IsActive && rule.Item.Code == normalizedCode)
            .Include(rule => rule.Item)
            .Select(rule => new MinimumStock
            {
                Code = rule.Item.Code,
                Name = rule.Item.Name,
                MinimumQuantity = rule.MinimumStock ?? 0,
                itemPriority = rule.ItemPriority
            })
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(MinimumStock item)
    {
        await EnsureLegacyMinimumsImportedAsync();

        var normalizedCode = item.Code.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            throw new InvalidOperationException("Informe o código do item.");

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var dbItem = await context.Items
            .Include(existingItem => existingItem.ReplenishmentRule)
            .FirstOrDefaultAsync(existingItem => existingItem.Code == normalizedCode);

        if (dbItem is null)
        {
            var stockProduct = await _stockService.GetByCodeAsync(normalizedCode);

            dbItem = new Item
            {
                Code = normalizedCode,
                Name = stockProduct?.Name ?? item.Name.Trim(),
                Unit = stockProduct?.Unit ?? string.Empty,
                ItemType = ItemTypeClassifier.Classify(stockProduct?.Name ?? item.Name),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Items.AddAsync(dbItem);
            await context.SaveChangesAsync();
        }
        else
        {
            var stockProduct = await _stockService.GetByCodeAsync(normalizedCode);
            dbItem.Name = stockProduct?.Name ?? item.Name.Trim();
            dbItem.Unit = stockProduct?.Unit ?? dbItem.Unit;
            dbItem.ItemType = ItemTypeClassifier.Classify(dbItem.Name);
            dbItem.IsActive = true;
            dbItem.UpdatedAt = DateTime.UtcNow;
        }

        if (dbItem.ReplenishmentRule is null)
        {
            dbItem.ReplenishmentRule = new ReplenishmentRule
            {
                Item = dbItem,
                MinimumStock = item.MinimumQuantity,
                CalculationMethod = ManualCalculationMethod,
                ItemPriority = item.itemPriority,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            dbItem.ReplenishmentRule.MinimumStock = item.MinimumQuantity;
            dbItem.ReplenishmentRule.CalculationMethod = ManualCalculationMethod;
            dbItem.ReplenishmentRule.ItemPriority = item.itemPriority;
            dbItem.ReplenishmentRule.IsActive = true;
            dbItem.ReplenishmentRule.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        _replenishmentDataState.NotifyChanged();
    }

    public async Task RemoveAsync(string code)
    {
        await EnsureLegacyMinimumsImportedAsync();

        var normalizedCode = code.Trim();
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var rule = await context.ReplenishmentRules
            .Include(replenishmentRule => replenishmentRule.Item)
            .FirstOrDefaultAsync(replenishmentRule => replenishmentRule.Item.Code == normalizedCode);

        if (rule is null)
            return;

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        _replenishmentDataState.NotifyChanged();
    }

    public async Task<List<ProductStock>> GetProductsWithoutMinimumAsync()
    {
        var products = await _stockService.GetAllAsync();
        var minimos = await GetAllAsync();
        var codesComMinimo = minimos.Select(m => m.Code).ToHashSet();

        return products.Where(p => !codesComMinimo.Contains(p.Code)).ToList();
    }

    private async Task EnsureLegacyMinimumsImportedAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        if (await context.ReplenishmentRules.AnyAsync())
            return;

        if (!File.Exists(_legacyFilePath))
            return;

        var json = await File.ReadAllTextAsync(_legacyFilePath);
        if (string.IsNullOrWhiteSpace(json))
            return;

        var legacyMinimums = JsonSerializer.Deserialize<List<MinimumStock>>(json, _jsonOptions);
        if (legacyMinimums is null || legacyMinimums.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var importedCodes = new HashSet<string>();

        foreach (var minimum in legacyMinimums)
        {
            var code = minimum.Code.Trim();
            if (string.IsNullOrWhiteSpace(code))
                continue;

            if (!importedCodes.Add(code))
                continue;

            var item = await context.Items
                .Include(existingItem => existingItem.ReplenishmentRule)
                .FirstOrDefaultAsync(existingItem => existingItem.Code == code);

            if (item is null)
            {
                item = new Item
                {
                    Code = code,
                    Name = minimum.Name.Trim(),
                    Unit = string.Empty,
                    ItemType = ItemTypeClassifier.Classify(minimum.Name),
                    IsActive = false,
                    CreatedAt = now
                };

                await context.Items.AddAsync(item);
            }

            item.ReplenishmentRule = new ReplenishmentRule
            {
                Item = item,
                MinimumStock = minimum.MinimumQuantity,
                CalculationMethod = ManualCalculationMethod,
                ItemPriority = minimum.itemPriority,
                IsActive = true,
                UpdatedAt = now
            };
        }

        await context.SaveChangesAsync();
    }
}
