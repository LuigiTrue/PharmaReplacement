using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class ReplenishmentService : IReplenishmentService
{
    private readonly IStockJsonService _stockService;
    private readonly IMinimumStockService _minimumStockService;
    private static readonly string[] ReplenishmentSources = { "1059", "999", "996" };

    // Threshold para Warning: estoque está até X% acima do mínimo
    private const decimal WarningThresholdPercent = 0.20m;

    public ReplenishmentService(
        IStockJsonService stockService,
        IMinimumStockService minimumStockService)
    {
        _stockService = stockService;
        _minimumStockService = minimumStockService;
    }

    public async Task<List<ReplenishmentItem>> GenerateAsync()
    {
        var products = await _stockService.GetAllAsync();
        var minimums = await _minimumStockService.GetAllAsync();

        var minimumMap = minimums.ToDictionary(m => m.Code);

        var result = new List<ReplenishmentItem>();

        foreach (var product in products)
        {
            if (!minimumMap.TryGetValue(product.Code, out var minimum))
                continue;

            // ✅ Usa apenas o estoque da farmácia central (997)
            var stockAt997 = GetStockAtLocation(product, "997");

            var priority = CalculatePriority(stockAt997, minimum.MinimumQuantity);

            if (priority == ReplenishmentPriority.Ok)
                continue;

            var recommendedBatch = SelectBatch(product.Batches);

            result.Add(new ReplenishmentItem
            {
                Code = product.Code,
                Name = product.Name,
                Unit = product.Unit,
                CurrentStock = stockAt997, // ✅ Exibe o estoque da farmácia, não o total
                MinimumQuantity = minimum.MinimumQuantity,
                MissingQuantity = Math.Max(0, minimum.MinimumQuantity - stockAt997),
                Priority = priority,
                ItemPriority = minimum.itemPriority,
                RecommendedBatch = recommendedBatch
            });
        }

        return result
                .OrderBy(r => r.ItemPriority)   // UltraHigh(0) → High(1) → Moderate(2) → Low(3)
                .ThenBy(r => r.Priority)        // Dentro de cada grupo, Critical antes de Warning
                .ThenBy(r => r.Name)            // Depois alfabético
                .ToList();
    }

    private ReplenishmentPriority CalculatePriority(decimal currentStock, decimal minimumQuantity)
    {
        if (currentStock <= minimumQuantity)
            return ReplenishmentPriority.Critical;

        var warningThreshold = minimumQuantity * (1 + WarningThresholdPercent);

        if (currentStock <= warningThreshold)
            return ReplenishmentPriority.Warning;

        return ReplenishmentPriority.Ok;
    }

    private BatchStock? SelectBatch(List<BatchStock> batches)
    {
        return batches
            .Where(b => IsAvailableForReplenishment(b))
            .OrderBy(b => b.Validity ?? DateTime.MaxValue)
            .FirstOrDefault();
    }

    private bool IsAvailableForReplenishment(BatchStock batch)
    {
        // O lote precisa ter quantidade disponível em pelo menos um dos locais de origem
        return batch.Locations
            .Any(l => ReplenishmentSources.Contains(l.LocationId) && l.Quantity > 0);
    }

    private decimal GetStockAtLocation(ProductStock product, string locationId)
    {
        return product.Batches
            .SelectMany(b => b.Locations)
            .Where(l => l.LocationId == locationId)
            .Sum(l => l.Quantity);
    }
}
