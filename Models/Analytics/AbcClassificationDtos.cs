using RepyPharma.Domain.Entities;

namespace RepyPharma.Models.Analytics;

/// <summary>
/// Período e conjunto de itens solicitados para uma análise ABC.
/// </summary>
public class AbcAnalysisRequestDto
{
    public DateRangeDto Period { get; set; } = new();
    public ItemFilterDto Items { get; set; } = new();
}

/// <summary>
/// Contexto de uma consulta ABC, por execução ou por período e conjunto de itens.
/// </summary>
public class AbcClassificationScopeDto
{
    public Guid? AnalysisId { get; set; }
    public DateRangeDto? Period { get; set; }
    public ItemFilterDto Items { get; set; } = new();
}

/// <summary>
/// Identificação e indicadores gerais de uma execução ABC.
/// </summary>
public class AbcAnalysisDto
{
    public Guid AnalysisId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public int ItemCount { get; set; }
    public decimal? TotalConsumptionValue { get; set; }
    public int ItemsWithMissingPrices { get; set; }
}

/// <summary>
/// Resultado ABC de um item vinculado à execução e ao seu período.
/// </summary>
public class AbcClassificationDto
{
    public long Id { get; set; }
    public Guid AnalysisId { get; set; }
    public ItemReferenceDto Item { get; set; } = new();
    public DateRangeDto Period { get; set; } = new();
    public int? Position { get; set; }
    public decimal ConsumptionValue { get; set; }
    public decimal AccumulatedPercentage { get; set; }
    public AbcClassification Classification { get; set; }
}

/// <summary>
/// Indicadores de uma das classes A, B ou C.
/// </summary>
public class AbcDistributionEntryDto
{
    public AbcClassification Classification { get; set; }
    public int ItemCount { get; set; }
    public decimal? ItemPercentage { get; set; }
    public decimal ConsumptionValue { get; set; }
    public decimal? FinancialPercentage { get; set; }
}

/// <summary>
/// Distribuição ABC do conjunto completo de uma análise.
/// </summary>
public class AbcDistributionDto
{
    public Guid AnalysisId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal TotalConsumptionValue { get; set; }
    public IReadOnlyList<AbcDistributionEntryDto> Classes { get; set; } = Array.Empty<AbcDistributionEntryDto>();
}

/// <summary>
/// Dados financeiros e acumulados de uma análise para apresentação do Pareto.
/// </summary>
public class ParetoAnalysisDto
{
    public Guid AnalysisId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public decimal TotalConsumptionValue { get; set; }
    public PagedResultDto<AbcClassificationDto> Items { get; set; } = new();
}

/// <summary>
/// Comparação das classes de um item entre duas análises identificadas.
/// </summary>
public class AbcClassificationChangeDto
{
    public ItemReferenceDto Item { get; set; } = new();
    public Guid ReferenceAnalysisId { get; set; }
    public Guid ComparisonAnalysisId { get; set; }
    public AbcClassification? ReferenceClassification { get; set; }
    public AbcClassification? ComparisonClassification { get; set; }
}
