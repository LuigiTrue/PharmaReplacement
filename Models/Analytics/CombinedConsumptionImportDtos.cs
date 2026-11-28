namespace RepyPharma.Models.Analytics;

public sealed record ConsumptionImportProgressDto(string Message);

/// <summary>Linha de preço do Soul MV. Setor/espécie são somente contexto da leitura.</summary>
public sealed record SectorConsumptionRecordDto
{
    public long RowNumber { get; init; }
    public string SectorCode { get; init; } = "";
    public string SectorName { get; init; } = "";
    public string SpeciesCode { get; init; } = "";
    public string SpeciesDescription { get; init; } = "";
    public string ProductCode { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal? UnitValue { get; init; }
    // Utilizados somente quando o produto possui preços positivos distintos.
    public decimal? MovementQuantity { get; init; }
    public decimal? MovementTotalValue { get; init; }
}

/// <summary>Preço positivo único ou custo ponderado setorial para a competência selecionada no card.</summary>
public sealed record MonthlyProductFinancialDto
{
    public string ProductCode { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal? UnitPurchasePrice { get; init; }
    public int SectorCount { get; init; }
}

public sealed record SectorConsumptionReportDto
{
    public string SourceFileName { get; init; } = "";
    public IReadOnlyList<SectorConsumptionRecordDto> Records { get; init; } = [];
    public IReadOnlyList<MonthlyProductFinancialDto> Products { get; init; } = [];
    public int SectorCount { get; init; }
    public IReadOnlyList<ConsumptionImportIssueDto> Issues { get; init; } = [];
}

public enum ConsumptionCorrespondence
{
    UnitPriceAvailable, UnknownProduct, FinancialUnavailable
}

public sealed record ConsumptionDiscrepancyDto
{
    public string ProductCode { get; init; } = "";
    public string Description { get; init; } = "";
    public string? DailyUnit { get; init; }
    public string? RegisteredUnit { get; init; }
    public decimal DailyQuantity { get; init; }
    public decimal? AverageDailyConsumption { get; init; }
    public decimal? UnitPurchasePrice { get; init; }
    public IReadOnlyList<ConsumptionCorrespondence> Categories { get; init; } = [];
    public IReadOnlyList<ConsumptionImportIssueDto> Issues { get; init; } = [];
}

public sealed record ConsumptionReportsValidationDto
{
    public Guid ValidationId { get; init; }
    public DateOnly ReferenceMonth { get; init; }
    public string DailyFileName { get; init; } = "";
    public string SectorFileName { get; init; } = "";
    public DateTimeOffset ExpiresAt { get; init; }
    public int DailyProductCount { get; init; }
    public int SectorProductCount { get; init; }
    public int DailyRecordCount { get; init; }
    public int MonthlyRecordCount { get; init; }
    public int AssignedPriceCount { get; init; }
    public int SignificantDiscrepancyCount { get; init; }
    public int MissingPriceCount { get; init; }
    public int UnknownProductCount { get; init; }
    public int SectorCount { get; init; }
    public IReadOnlyList<ConsumptionDiscrepancyDto> Products { get; init; } = [];
    public IReadOnlyList<ConsumptionImportIssueDto> Issues { get; init; } = [];
    public bool CanConfirm { get; init; }
}

public sealed class CombinedConsumptionImportResultDto : ConsumptionImportResultDto
{
    public Guid ValidationId { get; set; }
    public string SectorSourceFileName { get; set; } = "";
}
