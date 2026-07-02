using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class ReplenishmentDashboardService : IReplenishmentDashboardService
{
    private const string PharmacyCentralLocationId = "997";
    private const string PharmacyCentralName = "Farmácia Central";
    private const int TopReplenishmentItemsLimit = 10;
    private readonly IStockJsonService _stockService;
    private readonly IMinimumStockService _minimumStockService;

    public ReplenishmentDashboardService(
        IStockJsonService stockService,
        IMinimumStockService minimumStockService)
    {
        _stockService = stockService;
        _minimumStockService = minimumStockService;
    }

    public async Task<ReplenishmentDashboardData> GetDashboardDataAsync()
    {
        var products = await _stockService.GetAllAsync();
        var minimums = await _minimumStockService.GetAllAsync();
        var ignoredCodes = await _stockService.GetIgnoredCodesAsync();

        var productsByCode = products
            .GroupBy(p => p.Code)
            .ToDictionary(g => g.Key, g => g.First());

        var items = new List<ReplenishmentDashboardItem>();

        foreach (var minimum in minimums.Where(m => m.MinimumQuantity > 0))
        {
            if (ignoredCodes.Contains(minimum.Code))
                continue;

            productsByCode.TryGetValue(minimum.Code, out var product);

            var currentStock = product is null
                ? 0
                : GetStockAtLocation(product, PharmacyCentralLocationId);

            var coveredQuantity = Math.Min(currentStock, minimum.MinimumQuantity);
            var missingQuantity = Math.Max(0, minimum.MinimumQuantity - currentStock);
            var completionPercentage = minimum.MinimumQuantity == 0
                ? 100
                : coveredQuantity / minimum.MinimumQuantity * 100;
            var itemName = product?.Name ?? minimum.Name;

            items.Add(new ReplenishmentDashboardItem
            {
                Code = minimum.Code,
                Name = itemName,
                CurrentStock = currentStock,
                MinimumQuantity = minimum.MinimumQuantity,
                CoveredQuantity = coveredQuantity,
                MissingQuantity = missingQuantity,
                CompletionPercentage = Math.Round(completionPercentage, 1),
                SupplyPriorityRank = ReplenishmentPriorityPolicy.GetSupplyRank(itemName, minimum.itemPriority),
                SupplyPriorityGroup = ReplenishmentPriorityPolicy.GetSupplyGroupLabel(itemName, minimum.itemPriority)
            });
        }

        var requiredQuantity = items.Sum(i => i.MinimumQuantity);
        var coveredTotal = items.Sum(i => i.CoveredQuantity);
        var missingTotal = items.Sum(i => i.MissingQuantity);
        var completedItems = items.Count(i => !i.IsBelowMinimum);
        var belowMinimumItems = items.Count(i => i.IsBelowMinimum);
        var completionPercentageTotal = items.Count == 0
            ? 0
            : (decimal)completedItems / items.Count * 100;

        var orderedItems = items
            .OrderByDescending(i => i.IsBelowMinimum)
            .ThenBy(i => i.SupplyPriorityRank)
            .ThenBy(i => i.Name)
            .ToList();

        return new ReplenishmentDashboardData
        {
            LocationId = PharmacyCentralLocationId,
            LocationName = PharmacyCentralName,
            ReplenishmentCompletionPercentage = Math.Round(completionPercentageTotal, 1),
            RequiredQuantity = requiredQuantity,
            CoveredQuantity = coveredTotal,
            MissingQuantity = missingTotal,
            TotalItems = orderedItems.Count,
            CompletedItems = completedItems,
            BelowMinimumItems = belowMinimumItems,
            Items = orderedItems,
            CompletionChart = BuildCompletionChart(completedItems, belowMinimumItems),
            MissingByItemChart = BuildMissingByItemChart(orderedItems),
            TopReplenishmentItemsChart = BuildTopReplenishmentItemsChart(orderedItems)
        };
    }

    private static List<ReplenishmentDashboardChartPoint> BuildCompletionChart(int completedItems, int belowMinimumItems)
    {
        return new List<ReplenishmentDashboardChartPoint>
        {
            new() { Label = "Itens no mínimo ou acima", Value = completedItems },
            new() { Label = "Itens abaixo do mínimo", Value = belowMinimumItems }
        };
    }

    private static List<ReplenishmentDashboardChartPoint> BuildMissingByItemChart(List<ReplenishmentDashboardItem> items)
    {
        var missingItems = items
            .Where(i => i.IsBelowMinimum)
            .GroupBy(i => new { i.SupplyPriorityRank, i.SupplyPriorityGroup })
            .OrderBy(g => g.Key.SupplyPriorityRank)
            .Select(g => new ReplenishmentDashboardChartPoint
            {
                Label = g.Key.SupplyPriorityGroup,
                Value = g.Count()
            })
            .ToList();

        if (missingItems.Count > 0)
            return missingItems;

        return new List<ReplenishmentDashboardChartPoint>
        {
            new() { Label = "Sem pendências", Value = 0 }
        };
    }

    private static List<ReplenishmentDashboardChartPoint> BuildTopReplenishmentItemsChart(List<ReplenishmentDashboardItem> items)
    {
        var topItems = items
            .Where(i => i.IsBelowMinimum)
            .OrderBy(i => i.SupplyPriorityRank)
            .ThenByDescending(i => i.MissingQuantity)
            .ThenBy(i => i.Name)
            .Take(TopReplenishmentItemsLimit)
            .Select(i => new ReplenishmentDashboardChartPoint
            {
                Label = FormatChartLabel(i.Name),
                Value = i.MissingQuantity
            })
            .ToList();

        if (topItems.Count > 0)
            return topItems;

        return new List<ReplenishmentDashboardChartPoint>
        {
            new() { Label = "Sem pendências", Value = 0 }
        };
    }

    private static string FormatChartLabel(string label)
    {
        const int maxLength = 42;

        if (label.Length <= maxLength)
            return label;

        return $"{label[..(maxLength - 3)]}...";
    }

    private static decimal GetStockAtLocation(ProductStock product, string locationId)
    {
        return product.Batches
            .SelectMany(b => b.Locations)
            .Where(l => l.LocationId == locationId)
            .Sum(l => l.Quantity);
    }
}
