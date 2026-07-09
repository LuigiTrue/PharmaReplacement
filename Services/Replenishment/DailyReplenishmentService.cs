using Microsoft.EntityFrameworkCore;
using RepyPharma.Data;
using RepyPharma.Domain.Entities;
using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Replenishment;

public class DailyReplenishmentService(IDbContextFactory<AppDbContext> dbContextFactory) : IDailyReplenishmentService
{
    private const string PharmacyCentralLocationId = "997";
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<DailyReplenishmentData> GetDailyReplenishmentAsync(int coverageDays)
    {
        var normalizedCoverageDays = Math.Clamp(coverageDays, 1, 30);
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var items = await context.Items
            .AsNoTracking()
            .Where(item =>
                item.IsActive &&
                item.ItemType != ItemType.Controlled)
            .Include(item => item.StockBalances)
                .ThenInclude(balance => balance.Location)
            .ToListAsync();

        var latestAverages = (await context.ItemConsumptionAverages
                .AsNoTracking()
                .Where(average => average.MonthlyAverageOutput.HasValue)
                .OrderByDescending(average => average.ReportEndDate)
                .ThenByDescending(average => average.ImportedAt)
                .ToListAsync())
            .GroupBy(average => average.ItemCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var data = new DailyReplenishmentData
        {
            CoverageDays = normalizedCoverageDays,
            LastAverageImportedAt = latestAverages.Values
                .OrderByDescending(average => average.ImportedAt)
                .Select(average => (DateTime?)average.ImportedAt)
                .FirstOrDefault()
        };

        foreach (var item in items)
        {
            if (!HasStockBalanceAtLocation(item, PharmacyCentralLocationId))
                continue;

            latestAverages.TryGetValue(item.Code, out var average);

            var averageOutput = average?.MonthlyAverageOutput is null
                ? 0
                : Math.Floor(Math.Max(0, average.MonthlyAverageOutput.Value));

            var currentStock = GetReportStockBalance(average, item);
            var projectionDays = GetReportProjectionDays(average, currentStock, averageOutput);

            if (currentStock <= 0)
            {
                data.ZeroStockItems.Add(BuildItem(
                    item,
                    averageOutput,
                    currentStock,
                    projectionDays,
                    Math.Max(0, averageOutput * normalizedCoverageDays)));
            }

            if (averageOutput <= 0)
                continue;

            var suggestedQuantity = CalculateSuggestedQuantity(averageOutput, normalizedCoverageDays, currentStock);
            if (suggestedQuantity <= 0)
                continue;

            var replenishmentItem = BuildItem(
                item,
                averageOutput,
                currentStock,
                projectionDays,
                suggestedQuantity);

            if (replenishmentItem.IsMaterial)
                data.Materials.Add(replenishmentItem);
            else
                data.Medications.Add(replenishmentItem);
        }

        data.Materials = OrderItems(data.Materials);
        data.Medications = OrderItems(data.Medications);
        data.ZeroStockItems = data.ZeroStockItems
            .OrderBy(item => item.IsMaterial)
            .ThenBy(item => item.Name)
            .ToList();

        return data;
    }

    private static DailyReplenishmentItem BuildItem(
        Item item,
        decimal averageOutput,
        decimal currentStock,
        decimal projectionDays,
        decimal suggestedQuantity)
    {
        return new DailyReplenishmentItem
        {
            Code = item.Code,
            Name = item.Name,
            Unit = item.Unit,
            ItemType = item.ItemType,
            AverageOutput = averageOutput,
            CurrentStock = currentStock,
            ProjectionDays = projectionDays,
            SuggestedQuantity = Math.Floor(suggestedQuantity)
        };
    }

    private static List<DailyReplenishmentItem> OrderItems(List<DailyReplenishmentItem> items)
    {
        return items
            .OrderBy(item => item.ProjectionDays)
            .ThenByDescending(item => item.SuggestedQuantity)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private static decimal GetReportStockBalance(ItemConsumptionAverage? average, Item item)
    {
        if (average?.StockBalance.HasValue == true)
            return Math.Max(0, average.StockBalance.Value);

        return GetStockAtLocation(item, PharmacyCentralLocationId);
    }

    private static decimal GetReportProjectionDays(
        ItemConsumptionAverage? average,
        decimal currentStock,
        decimal averageOutput)
    {
        if (average?.ProjectedCoverageDays.HasValue == true)
            return Math.Max(0, average.ProjectedCoverageDays.Value);

        return averageOutput <= 0
            ? 0
            : currentStock / averageOutput;
    }

    private static decimal CalculateSuggestedQuantity(
        decimal averageOutput,
        int coverageDays,
        decimal currentStock)
    {
        return Math.Max(0, averageOutput * coverageDays - currentStock);
    }

    private static decimal GetStockAtLocation(Item item, string locationId)
    {
        return item.StockBalances
            .Where(balance => balance.Location.Code == locationId)
            .Sum(balance => balance.Quantity);
    }

    private static bool HasStockBalanceAtLocation(Item item, string locationId)
    {
        return item.StockBalances.Any(balance => balance.Location.Code == locationId);
    }
}
