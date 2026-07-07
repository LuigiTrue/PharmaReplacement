using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain;
using RepyPharma.Domain.Entities;

namespace RepyPharma.Infrastructure.Json;

public class PdfStorageService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ReplenishmentDataState replenishmentDataState)
{
    private static readonly Dictionary<string, string> LocationNames = new()
    {
        { "996", "Almoxarifado" },
        { "997", "Farmacia Central" },
        { "998", "Farmacia Centro Cirurgico" },
        { "999", "CAF" },
        { "1059", "Fracionamento" }
    };

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly ReplenishmentDataState _replenishmentDataState = replenishmentDataState;

    public async Task SaveAsync(List<ProductStock> produtos)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var importedItemCodes = new HashSet<string>();
        var importedBalanceKeys = new HashSet<StockBalanceKey>();

        var itemsByCode = await context.Items.ToDictionaryAsync(item => item.Code);
        var locationsByCode = await context.Locations.ToDictionaryAsync(location => location.Code);

        foreach (var produto in produtos)
        {
            var code = produto.Code.Trim();
            if (string.IsNullOrWhiteSpace(code))
                continue;

            importedItemCodes.Add(code);

            if (!itemsByCode.TryGetValue(code, out var item))
            {
                item = new Item
                {
                    Code = code,
                    Name = produto.Name.Trim(),
                    Unit = produto.Unit.Trim(),
                    ItemType = ItemTypeClassifier.Classify(produto.Name),
                    IsActive = true,
                    CreatedAt = now
                };

                await context.Items.AddAsync(item);
                await context.SaveChangesAsync();
                itemsByCode[item.Code] = item;
            }
            else
            {
                item.Name = produto.Name.Trim();
                item.Unit = produto.Unit.Trim();
                item.ItemType = ItemTypeClassifier.Classify(produto.Name);
                item.IsActive = true;
                item.UpdatedAt = now;
            }

            foreach (var lote in produto.Batches)
            {
                var batchNumber = lote.Batch.Trim();
                if (string.IsNullOrWhiteSpace(batchNumber) || lote.Validity is null)
                    continue;

                var validity = ToUtc(lote.Validity.Value);
                var batch = await context.Batches
                    .FirstOrDefaultAsync(existingBatch =>
                        existingBatch.ItemId == item.Id &&
                        existingBatch.BatchNumber == batchNumber);

                if (batch is null)
                {
                    batch = new Batch
                    {
                        Item = item,
                        BatchNumber = batchNumber,
                        Validity = validity,
                        CreatedAt = now
                    };

                    await context.Batches.AddAsync(batch);
                    await context.SaveChangesAsync();
                }
                else
                {
                    batch.Validity = validity;
                    batch.UpdatedAt = now;
                }

                foreach (var local in lote.Locations)
                {
                    var locationCode = local.LocationId.Trim();
                    if (string.IsNullOrWhiteSpace(locationCode))
                        continue;

                    if (!locationsByCode.TryGetValue(locationCode, out var location))
                    {
                        location = new Location
                        {
                            Code = locationCode,
                            Name = GetLocationName(locationCode),
                            IsActive = true
                        };

                        await context.Locations.AddAsync(location);
                        await context.SaveChangesAsync();
                        locationsByCode[location.Code] = location;
                    }
                    else
                    {
                        location.Name = GetLocationName(locationCode);
                        location.IsActive = true;
                    }

                    importedBalanceKeys.Add(new StockBalanceKey(item.Id, batch.Id, location.Id));

                    var stockBalance = await context.StockBalances
                        .FirstOrDefaultAsync(balance =>
                            balance.ItemId == item.Id &&
                            balance.BatchId == batch.Id &&
                            balance.LocationId == location.Id);

                    if (stockBalance is null)
                    {
                        stockBalance = new StockBalance
                        {
                            Item = item,
                            Batch = batch,
                            Location = location,
                            Quantity = local.Quantity,
                            UpdatedAt = now
                        };

                        await context.StockBalances.AddAsync(stockBalance);
                    }
                    else
                    {
                        stockBalance.Quantity = local.Quantity;
                        stockBalance.UpdatedAt = now;
                    }
                }
            }
        }

        var staleBalances = await context.StockBalances.ToListAsync();
        context.StockBalances.RemoveRange(staleBalances
            .Where(balance => !importedBalanceKeys.Contains(
                new StockBalanceKey(balance.ItemId, balance.BatchId, balance.LocationId))));

        foreach (var item in itemsByCode.Values.Where(item => !importedItemCodes.Contains(item.Code)))
        {
            item.IsActive = false;
            item.UpdatedAt = now;
        }

        await context.SaveChangesAsync();

        var orphanBatches = await context.Batches
            .Where(batch => !context.StockBalances.Any(balance => balance.BatchId == batch.Id))
            .ToListAsync();

        context.Batches.RemoveRange(orphanBatches);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        _replenishmentDataState.NotifyChanged();
    }

    private static string GetLocationName(string code)
    {
        return LocationNames.TryGetValue(code, out var name) ? name : code;
    }

    private static DateTime ToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Utc)
            return date;

        if (date.Kind == DateTimeKind.Local)
            return date.ToUniversalTime();

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private readonly record struct StockBalanceKey(int ItemId, int BatchId, int LocationId);
}
