namespace RepyPharma.Models.Analytics;

/// <summary>
/// Abrangência temporal dos dados para o conjunto e período consultados.
/// </summary>
public class ConsumptionDataCoverageDto
{
    public DateRangeDto RequestedPeriod { get; set; } = new();
    public DateOnly? FirstAvailableDate { get; set; }
    public DateOnly? LastAvailableDate { get; set; }
    public int ItemCount { get; set; }
    public long RecordedPeriodCount { get; set; }
    public long ExpectedPeriodCount { get; set; }
    public decimal? CoveragePercentage { get; set; }
}

/// <summary>
/// Intervalo diário ou mensal sem registro para um item.
/// </summary>
public class MissingConsumptionPeriodDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public ConsumptionGranularity Granularity { get; set; }
}

/// <summary>
/// Histórico disponível de um item em relação ao mínimo solicitado.
/// </summary>
public class InsufficientHistoryItemDto
{
    public ItemReferenceDto Item { get; set; } = new();
    public int AvailableHistoryDays { get; set; }
    public int RequiredHistoryDays { get; set; }
    public DateOnly? FirstAvailableDate { get; set; }
    public DateOnly? LastAvailableDate { get; set; }
}

/// <summary>
/// Indicadores de abrangência, ausência de períodos e preços disponíveis.
/// </summary>
public class DataQualitySummaryDto
{
    public DateRangeDto Period { get; set; } = new();
    public int ItemCount { get; set; }
    public int ItemsWithHistory { get; set; }
    public int ItemsWithInsufficientHistory { get; set; }
    public long MissingPeriodCount { get; set; }
    public int ItemsWithMissingPrices { get; set; }
    public decimal? CoveragePercentage { get; set; }
    public IReadOnlyList<string> Messages { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Metadados de última importação, somente quando uma fonte estiver disponível.
/// </summary>
public class LastConsumptionImportDto
{
    public Guid? ImportId { get; set; }
    public string? SourceFileName { get; set; }
    public DateTimeOffset? ImportedAt { get; set; }
    public DateRangeDto? ConsumptionPeriod { get; set; }
    public long? ImportedRowCount { get; set; }
}
