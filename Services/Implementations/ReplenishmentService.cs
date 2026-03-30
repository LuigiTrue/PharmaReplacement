using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class ReplenishmentService : IReplenishmentService
{
    private readonly IStockJsonService _stockService;
    private readonly IMinimumStockService _minimumStockService;

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
            // Ignora produtos sem mínimo cadastrado
            if (!minimumMap.TryGetValue(product.Code, out var minimum))
                continue;

            var priority = CalculatePriority(product.TotalStock, minimum.MinimumQuantity);

            // Apenas Critical e Warning entram no relatório
            if (priority == ReplenishmentPriority.Ok)
                continue;

            var recommendedBatch = SelectBatch(product.Batches);

            result.Add(new ReplenishmentItem
            {
                Code = product.Code,
                Name = product.Name,
                Unit = product.Unit,
                CurrentStock = product.TotalStock,
                MinimumQuantity = minimum.MinimumQuantity,
                MissingQuantity = Math.Max(0, minimum.MinimumQuantity - product.TotalStock),
                Priority = priority,
                RecommendedBatch = recommendedBatch
            });
        }

        // Críticos primeiro, depois Warning. Dentro de cada grupo, ordena por nome
        return result
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
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
        // FEFO: seleciona o lote com menor validade (mais próximo do vencimento)
        // Lotes sem validade ficam por último
        return batches
            .Where(b => b.Locations.Sum(l => l.Quantity) > 0)
            .OrderBy(b => b.Validity ?? DateTime.MaxValue)
            .FirstOrDefault();
    }
}
