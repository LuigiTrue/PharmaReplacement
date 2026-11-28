using RepyPharma.Domain.Entities;

namespace RepyPharma.Models.Analytics;

/// <summary>
/// Consumo diário registrado de um item.
/// </summary>
public class DailyConsumptionDto
{
    public long Id { get; set; }
    public int? ItemId { get; set; }
    public string? SoulMvProductCode { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public DateOnly Date { get; set; }
    public decimal Quantity { get; set; }
}

/// <summary>
/// Consumo mensal e preço unitário do respectivo mês.
/// </summary>
public class MonthlyConsumptionDto
{
    public long Id { get; set; }
    public int? ItemId { get; set; }
    public string? SoulMvProductCode { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal? AverageDailyConsumption { get; set; }
    public int RecordedDays { get; set; }
    public bool IsComplete { get; set; }
    public decimal? ReportedTotalQuantity { get; set; }
    public decimal? ReportedAverageDailyConsumption { get; set; }
    public decimal? UnitPurchasePrice { get; set; }
}

/// <summary>
/// Indicadores de consumo de um item no intervalo solicitado.
/// </summary>
public class ConsumptionSummaryDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public decimal TotalQuantity { get; set; }
    public decimal? AverageDailyConsumption { get; set; }
    public decimal? MinimumDailyConsumption { get; set; }
    public decimal? MaximumDailyConsumption { get; set; }
    public int RecordedDays { get; set; }
    public int DaysWithConsumption { get; set; }
    public decimal? ConsumptionValue { get; set; }
}

/// <summary>
/// Média de consumo e quantidade de observações utilizadas.
/// </summary>
public class AverageConsumptionDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public ConsumptionGranularity Granularity { get; set; }
    public int ObservationCount { get; set; }
    public decimal? AverageQuantity { get; set; }
}

/// <summary>
/// Variação de consumo entre dois intervalos explícitos.
/// </summary>
public class ConsumptionVariationDto
{
    public int ItemId { get; set; }
    public DateRangeDto ReferencePeriod { get; set; } = new();
    public DateRangeDto ComparisonPeriod { get; set; } = new();
    public decimal? ReferenceQuantity { get; set; }
    public decimal? ComparisonQuantity { get; set; }
    public decimal? VariationPercentage { get; set; }
}

/// <summary>
/// Ponto de uma série histórica ou de indicadores de quantidade.
/// </summary>
public class ConsumptionSeriesPointDto
{
    public DateOnly Date { get; set; }
    public decimal? Quantity { get; set; }
}

/// <summary>
/// Série de médias móveis com janela e granularidade identificadas.
/// </summary>
public class MovingAverageDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public int WindowSize { get; set; }
    public ConsumptionGranularity Granularity { get; set; }
    public IReadOnlyList<ConsumptionSeriesPointDto> Points { get; set; } = Array.Empty<ConsumptionSeriesPointDto>();
}

/// <summary>
/// Séries e variação entre períodos equivalentes de anos distintos.
/// </summary>
public class YearOverYearComparisonDto
{
    public int ItemId { get; set; }
    public ConsumptionVariationDto OverallVariation { get; set; } = new();
    public IReadOnlyList<ConsumptionSeriesPointDto> ReferenceSeries { get; set; } = Array.Empty<ConsumptionSeriesPointDto>();
    public IReadOnlyList<ConsumptionSeriesPointDto> ComparisonSeries { get; set; } = Array.Empty<ConsumptionSeriesPointDto>();
}

/// <summary>
/// Posição de um item em um ranking por volume consumido.
/// </summary>
public class ConsumptionRankingDto
{
    public int Position { get; set; }
    public ItemReferenceDto Item { get; set; } = new();
    public decimal TotalQuantity { get; set; }
    public decimal? ConsumptionValue { get; set; }
}

/// <summary>
/// Consolidação de consumo por classificação atual do produto.
/// </summary>
public class ConsumptionGroupDto
{
    public ItemType ItemType { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal? ConsumptionValue { get; set; }
}
