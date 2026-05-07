using RepyPharma.Models;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

public class ReplenishmentService : IReplenishmentService
{
    private readonly IStockJsonService _stockService;
    private readonly IMinimumStockService _minimumStockService;
    private static readonly string[] ReplenishmentSources = { "1059", "999", "996" };
    private const string Fractionation = "1059";
    private const string Caf = "999";
    private const string Stock = "996";
    private const string Pharmacy = "997";

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
        var ignoredCodes = await _stockService.GetIgnoredCodesAsync();

        var minimumMap = minimums.ToDictionary(m => m.Code);

        var result = new List<ReplenishmentItem>();

        foreach (var product in products)
        {
            if (!minimumMap.TryGetValue(product.Code, out var minimum))
                continue;
            if (ignoredCodes.Contains(product.Code))
                continue;

            // ✅ Usa apenas o estoque da farmácia central (997)
            var stockAt997 = GetStockAtLocation(product, "997");

            var priority = CalculatePriority(stockAt997, minimum.MinimumQuantity);

            if (priority == ReplenishmentPriority.Ok)
                continue;

            var recommendedBatch = SelectBatch(product.Batches);
            var availableBatches = GetAvailableBatches(product.Batches);

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
                RecommendedBatch = recommendedBatch,
                AvailableBatches = availableBatches
            });
        }

        return result
                .OrderBy(r => r.ItemPriority)   // UltraHigh(0) → High(1) → Moderate(2) → Low(3)
                .ThenBy(r => r.Priority)        // Dentro de cada grupo, Critical antes de Warning
                .ThenBy(r => r.Name)            // Depois alfabético
                .ToList();
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync()
    {
        var products = await _stockService.GetAllAsync();
        var minimums = await _minimumStockService.GetAllAsync();
        var minimumMap = minimums.ToDictionary(m => m.Code);

        var needToBuy = new List<ReplenishmentItem>();
        var runningLow = new List<ReplenishmentItem>();
        var aboveNormal = new List<ReplenishmentItem>();

        foreach (var product in products)
        {
            if (!minimumMap.TryGetValue(product.Code, out var minimum))
                continue;

            var stockAt997 = GetStockAtLocation(product, "997");
            var priority = CalculatePriority(stockAt997, minimum.MinimumQuantity);
            var recommended = SelectBatch(product.Batches);

            var item = new ReplenishmentItem
            {
                Code = product.Code,
                Name = product.Name,
                Unit = product.Unit,
                CurrentStock = stockAt997,
                MinimumQuantity = minimum.MinimumQuantity,
                MissingQuantity = Math.Max(0, minimum.MinimumQuantity - stockAt997),
                Priority = priority,
                ItemPriority = minimum.itemPriority,
                RecommendedBatch = recommended

            };

            if (priority == ReplenishmentPriority.Critical)
                needToBuy.Add(item);
            else if (priority == ReplenishmentPriority.Warning)
                runningLow.Add(item);
            else if (IsAboveNormal(stockAt997, minimum.MinimumQuantity))
                aboveNormal.Add(item);
        }

        var order = (List<ReplenishmentItem> list) => list
            .OrderBy(i => i.ItemPriority)
            .ThenBy(i => i.Name)
            .ToList();

        return new DashboardSummary
        {
            NeedToBuy = order(needToBuy),
            RunningLow = order(runningLow),
            AboveNormal = order(aboveNormal)
        };
    }

    // Considera acima do normal quando estoque é 2x maior que o mínimo
    private bool IsAboveNormal(decimal currentStock, decimal minimumQuantity)
    {
        return minimumQuantity > 0 && currentStock >= minimumQuantity * 2;
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

    private List<BatchStock> GetAvailableBatches(List<BatchStock> batches)
    {
        return batches
            .Where(b => IsAvailableForReplenishment(b))
            .OrderBy(b => b.Validity ?? DateTime.MaxValue)
            .ToList();
    }

    public async Task<ReplenishmentReport> GenerateReportAsync()
{
    var items = await GenerateAsync();
    var report = new ReplenishmentReport();

    foreach (var item in items)
    {
        // Verifica em quais fontes o item tem lotes disponíveis
        var inFractionation = HasStockAt(item.AvailableBatches, Fractionation);
        var inCaf           = HasStockAt(item.AvailableBatches, Caf);
        var inStock         = HasStockAt(item.AvailableBatches, Stock);

        // Filtra os lotes por seção e verifica conflito de lote
        if (inFractionation)
        {
            item.AvailableBatches = FilterBatchesByLocation(item.AvailableBatches, Fractionation);
            item.HasLotConflict   = CheckLotConflict(item);
            report.FromFractionation.Add(item);
        }
        else if (inCaf)
        {
            item.AvailableBatches = FilterBatchesByLocation(item.AvailableBatches, Caf);
            item.HasLotConflict   = CheckLotConflict(item);
            report.FromCafOnly.Add(item);
        }
        else if (inStock)
        {
            item.AvailableBatches = FilterBatchesByLocation(item.AvailableBatches, Stock);
            item.HasLotConflict   = CheckLotConflict(item);
            report.FromStockOnly.Add(item);
        }
        else
        {
            report.NoSourceAvailable.Add(item);
        }
    }

    return report;
}

// Verifica se há lotes disponíveis em uma localização específica
private bool HasStockAt(List<BatchStock> batches, string locationId)
{
    return batches.Any(b =>
        b.Locations.Any(l => l.LocationId == locationId && l.Quantity > 0));
}

// Retorna apenas os lotes que possuem estoque na localização informada
private List<BatchStock> FilterBatchesByLocation(List<BatchStock> batches, string locationId)
{
    return batches
        .Where(b => b.Locations.Any(l => l.LocationId == locationId && l.Quantity > 0))
        .OrderBy(b => b.Validity ?? DateTime.MaxValue)
        .ToList();
}

// Conflito: item existe na farmácia (997), mas nenhum lote disponível
// para reposição tem o mesmo número de lote que já está lá
private bool CheckLotConflict(ReplenishmentItem item)
{
    var batchesAt997 = item.RecommendedBatch is not null
        ? new HashSet<string> { item.RecommendedBatch.Batch }
        : new HashSet<string>();

    if (!batchesAt997.Any())
        return false;

    var availableBatchNumbers = item.AvailableBatches
        .Select(b => b.Batch)
        .ToHashSet();

    // Conflito se nenhum lote disponível coincide com o que já está na farmácia
    return !batchesAt997.Any(b => availableBatchNumbers.Contains(b));
}
}
