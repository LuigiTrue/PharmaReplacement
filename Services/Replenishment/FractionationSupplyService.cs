using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Replenishment;

public class FractionationSupplyService(IDbContextFactory<AppDbContext> dbContextFactory) : IFractionationSupplyService
{
    private const string FractionationLocationId = "1059";
    private const string PharmacyLocationId = "997";
    private static readonly string[] SourceLocationIds = { "999", "996" };

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<FractionationSupplyData> GetSupplyDataAsync(int coverageDays)
    {
        var normalizedCoverageDays = Math.Clamp(coverageDays, 1, 30);
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var items = await context.Items
            .AsNoTracking()
            .Where(item =>
                item.IsActive &&
                item.ItemType != ItemType.Psychotropic &&
                item.ItemType != ItemType.Sedative)
            .Include(item => item.ReplenishmentRule)
            .Include(item => item.StockBalances)
                .ThenInclude(balance => balance.Batch)
            .Include(item => item.StockBalances)
                .ThenInclude(balance => balance.Location)
            .ToListAsync();

        var itemsByCode = items.ToDictionary(item => item.Code, StringComparer.OrdinalIgnoreCase);

        var latestAverages = (await context.ItemConsumptionAverages
                .AsNoTracking()
                .OrderByDescending(average => average.ReportEndDate)
                .ThenByDescending(average => average.ImportedAt)
                .ToListAsync())
            .GroupBy(average => average.ItemCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var replenishmentItems = new List<FractionationSupplyItem>();
        var minimumShortageItems = new List<FractionationSupplyItem>();

        foreach (var item in items)
        {
            latestAverages.TryGetValue(item.Code, out var average);

            var weeklyAverage = average is null
                ? 0
                : GetWeeklyAverageOutput(average);
            var dailyAverage = weeklyAverage / 7;
            var requiredQuantity = Math.Ceiling(dailyAverage * normalizedCoverageDays);
            var fractionationStock = GetStockAtLocation(item, FractionationLocationId);
            var pharmacyStock = GetStockAtLocation(item, PharmacyLocationId);
            var minimumQuantity = item.ReplenishmentRule?.IsActive == true
                ? item.ReplenishmentRule.MinimumStock ?? 0
                : 0;

            if (weeklyAverage > 0)
            {
                var suggestedQuantity = Math.Max(0, requiredQuantity - fractionationStock);
                if (suggestedQuantity <= 0)
                    continue;

                replenishmentItems.Add(BuildSupplyItem(
                    item,
                    average,
                    weeklyAverage,
                    dailyAverage,
                    requiredQuantity,
                    fractionationStock,
                    pharmacyStock,
                    minimumQuantity,
                    suggestedQuantity));

                continue;
            }

            if (minimumQuantity <= 0 || pharmacyStock > minimumQuantity * 0.5m)
                continue;

            minimumShortageItems.Add(BuildSupplyItem(
                item,
                average,
                weeklyAverage,
                dailyAverage,
                minimumQuantity,
                fractionationStock,
                pharmacyStock,
                minimumQuantity,
                Math.Max(0, minimumQuantity - pharmacyStock)));
        }

        return new FractionationSupplyData
        {
            CoverageDays = normalizedCoverageDays,
            ReplenishmentItems = replenishmentItems
                .OrderByDescending(item => item.SuggestedQuantity)
                .ThenBy(item => item.Name)
                .ToList(),
            MinimumShortageItems = minimumShortageItems
                .OrderBy(item => item.CurrentPharmacyStock)
                .ThenByDescending(item => item.MinimumQuantity)
                .ThenBy(item => item.Name)
                .ToList()
        };
    }

    private static FractionationSupplyItem BuildSupplyItem(
        Item item,
        ItemConsumptionAverage? average,
        decimal weeklyAverage,
        decimal dailyAverage,
        decimal requiredQuantity,
        decimal fractionationStock,
        decimal pharmacyStock,
        decimal minimumQuantity,
        decimal suggestedQuantity)
    {
        var availableBatches = GetAvailableBatches(item.StockBalances);

        return new FractionationSupplyItem
        {
            Code = item.Code,
            Name = item.Name,
            Unit = item.Unit,
            ItemType = item.ItemType,
            WeeklyAverageOutput = weeklyAverage,
            DailyAverageOutput = dailyAverage,
            RequiredQuantity = requiredQuantity,
            CurrentFractionationStock = fractionationStock,
            CurrentPharmacyStock = pharmacyStock,
            MinimumQuantity = minimumQuantity,
            SuggestedQuantity = suggestedQuantity,
            AverageReferenceStartDate = average?.ReportStartDate,
            AverageReferenceEndDate = average?.ReportEndDate,
            AveragePeriodKind = average?.AveragePeriodKind ?? "",
            RecommendedBatch = availableBatches.FirstOrDefault(),
            AvailableBatches = availableBatches
        };
    }

    private static decimal GetWeeklyAverageOutput(ItemConsumptionAverage average)
    {
        if (average.WeeklyAverageOutput.HasValue)
            return Math.Max(0, average.WeeklyAverageOutput.Value);

        if (average.MonthlyAverageOutput.HasValue && average.CoverageDays > 0)
            return Math.Max(0, average.MonthlyAverageOutput.Value / average.CoverageDays * 7);

        if (average.CurrentAverageOutput.HasValue && average.CoverageDays > 0)
            return Math.Max(0, average.CurrentAverageOutput.Value / average.CoverageDays * 7);

        return 0;
    }

    private static decimal GetStockAtLocation(Item item, string locationId)
    {
        return item.StockBalances
            .Where(balance => balance.Location.Code == locationId)
            .Sum(balance => balance.Quantity);
    }

    private static List<BatchStock> GetAvailableBatches(IEnumerable<StockBalance> stockBalances)
    {
        return stockBalances
            .Where(balance => SourceLocationIds.Contains(balance.Location.Code) && balance.Quantity > 0)
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
            .ToList();
    }
}
