using RepyPharma.Domain.Entities;

namespace RepyPharma.Models.Analytics;

/// <summary>
/// Recorte de consumo, análise ABC e filtros de criticidade e estoque.
/// </summary>
public class InventoryAnalysisQueryDto
{
    public ConsumptionQueryDto Consumption { get; set; } = new();
    public Guid? AbcAnalysisId { get; set; }
    public XyzCriticality? Criticality { get; set; }
    public IReadOnlyList<int>? StockLocationIds { get; set; }
    public AnalysisSeverity? MinimumAlertSeverity { get; set; }
}

/// <summary>
/// Indicadores gerais do conjunto de itens consultado.
/// </summary>
public class ItemsOverviewDto
{
    public DateRangeDto Period { get; set; } = new();
    public int ItemCount { get; set; }
    public int ItemsWithConsumption { get; set; }
    public int ItemsWithoutHistory { get; set; }
    public int ItemsWithoutCriticality { get; set; }
    public int ItemsWithAlerts { get; set; }
    public decimal? TotalConsumptionValue { get; set; }
}

/// <summary>
/// Dados compostos para análise individual, sem expor entidades EF.
/// </summary>
public class ItemAnalysisDto
{
    public ItemReferenceDto Item { get; set; } = new();
    public ConsumptionSummaryDto? Consumption { get; set; }
    public AbcClassificationDto? AbcClassification { get; set; }
    public ItemCriticalityDto? Criticality { get; set; }
    public ConsumptionForecastDto? Forecast { get; set; }
    public StockCoverageDto? StockCoverage { get; set; }
    public DataQualitySummaryDto? DataQuality { get; set; }
    public IReadOnlyList<ConsumptionAlertDto> Alerts { get; set; } = Array.Empty<ConsumptionAlertDto>();
}

/// <summary>
/// Contagem e valor de uma combinação ABC e XYZ, admitindo ausência de classificação.
/// </summary>
public class AbcXyzMatrixCellDto
{
    public AbcClassification? AbcClassification { get; set; }
    public XyzCriticality? Criticality { get; set; }
    public int ItemCount { get; set; }
    public decimal? ConsumptionValue { get; set; }
}

/// <summary>
/// Distribuição combinada do conjunto de uma análise ABC com a criticidade manual.
/// </summary>
public class AbcXyzMatrixDto
{
    public Guid AbcAnalysisId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public IReadOnlyList<AbcXyzMatrixCellDto> Cells { get; set; } = Array.Empty<AbcXyzMatrixCellDto>();
}

/// <summary>
/// Alerta ou desvio de consumo com valores de referência opcionais.
/// </summary>
public class ConsumptionAlertDto
{
    public ItemReferenceDto Item { get; set; } = new();
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AnalysisSeverity Severity { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public decimal? ObservedValue { get; set; }
    public decimal? ReferenceValue { get; set; }
    public decimal? VariationPercentage { get; set; }
}

/// <summary>
/// Indicadores financeiros e disponibilidade de preços no período selecionado.
/// </summary>
public class FinancialConsumptionSummaryDto
{
    public DateRangeDto Period { get; set; } = new();
    public decimal? TotalConsumptionValue { get; set; }
    public decimal? AverageMonthlyConsumptionValue { get; set; }
    public int PricedItemCount { get; set; }
    public int ItemsWithMissingPrices { get; set; }
}
