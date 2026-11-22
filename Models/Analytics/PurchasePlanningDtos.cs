using RepyPharma.Domain.Entities;

namespace RepyPharma.Models.Analytics;

/// <summary>
/// Horizonte e parâmetros para futuros cálculos de compra, sem valores de política predefinidos.
/// </summary>
public class PurchasePlanningParametersDto
{
    public DateOnly ReferenceDate { get; set; }
    public int HorizonDays { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? TargetServiceLevelPercentage { get; set; }
    public ForecastMethodOptionsDto? ForecastMethod { get; set; }
    public Guid? AbcAnalysisId { get; set; }
    public IReadOnlyList<int>? StockLocationIds { get; set; }
}

/// <summary>
/// Necessidade projetada e estimativa financeira de aquisição de um item.
/// </summary>
public class PurchaseRequirementDto
{
    public int ItemId { get; set; }
    public DateOnly ReferenceDate { get; set; }
    public int HorizonDays { get; set; }
    public decimal? AvailableQuantity { get; set; }
    public decimal? ForecastQuantity { get; set; }
    public decimal? RequiredQuantity { get; set; }
    public decimal? EstimatedUnitPurchasePrice { get; set; }
    public decimal? EstimatedPurchaseValue { get; set; }
    public DateOnly? SuggestedOrderDate { get; set; }
}

/// <summary>
/// Indicadores de saldo, consumo e autonomia de um item.
/// </summary>
public class StockCoverageDto
{
    public int ItemId { get; set; }
    public DateOnly ReferenceDate { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal? AverageDailyConsumption { get; set; }
    public decimal? CoverageDays { get; set; }
    public int? LeadTimeDays { get; set; }
}

/// <summary>
/// Dados de referência de um futuro cálculo de ponto de reposição.
/// </summary>
public class ReorderPointDto
{
    public int ItemId { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? AverageDailyConsumption { get; set; }
    public decimal? SafetyStockQuantity { get; set; }
    public decimal? ReorderPointQuantity { get; set; }
}

/// <summary>
/// Quantidade de segurança e parâmetros utilizados pelo futuro método.
/// </summary>
public class SafetyStockDto
{
    public int ItemId { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? TargetServiceLevelPercentage { get; set; }
    public decimal? SafetyStockQuantity { get; set; }
    public string? MethodCode { get; set; }
}

/// <summary>
/// Plano de aquisição de um item com prioridade, autonomia e classificações.
/// </summary>
public class PurchasePlanningDto
{
    public ItemReferenceDto Item { get; set; } = new();
    public PurchaseRequirementDto Requirement { get; set; } = new();
    public ItemPriorityDto Priority { get; set; } = new();
    public StockCoverageDto? StockCoverage { get; set; }
    public AbcClassification? AbcClassification { get; set; }
    public XyzCriticality? Criticality { get; set; }
}

/// <summary>
/// Indicadores gerais de um horizonte de planejamento de compras.
/// </summary>
public class PurchasePlanningSummaryDto
{
    public DateOnly ReferenceDate { get; set; }
    public int HorizonDays { get; set; }
    public int ItemCount { get; set; }
    public int ItemsRequiringPurchase { get; set; }
    public decimal? TotalEstimatedPurchaseValue { get; set; }
    public int ItemsWithMissingPrices { get; set; }
    public int ItemsWithInsufficientHistory { get; set; }
}
