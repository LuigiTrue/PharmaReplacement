namespace RepyPharma.Models.Analytics;

/// <summary>
/// Opções de formato CSV a serem interpretadas pelo futuro importador.
/// </summary>
public class ConsumptionImportOptionsDto
{
    public string? Delimiter { get; set; }
    public string? CultureName { get; set; }
    public string? DateFormat { get; set; }
    public bool? HasHeader { get; set; }
}

/// <summary>
/// Problema de validação associado a uma linha ou coluna do arquivo.
/// </summary>
public class ConsumptionImportIssueDto
{
    public long? RowNumber { get; set; }
    public string? ColumnName { get; set; }
    public string? ItemCode { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AnalysisSeverity Severity { get; set; }
}

/// <summary>
/// Resultado da validação de estrutura e conteúdo de um arquivo CSV.
/// </summary>
public class ConsumptionImportValidationDto
{
    public bool IsValid { get; set; }
    public long ReadRowCount { get; set; }
    public long ValidRowCount { get; set; }
    public DateRangeDto? DetectedPeriod { get; set; }
    public IReadOnlyList<ConsumptionImportIssueDto> Issues { get; set; } = Array.Empty<ConsumptionImportIssueDto>();
}

/// <summary>
/// Identificação, estado e contadores de uma importação de consumo.
/// </summary>
public class ConsumptionImportResultDto
{
    public Guid ImportId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public ConsumptionOperationStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long ReadRowCount { get; set; }
    public long ImportedRowCount { get; set; }
    public long RejectedRowCount { get; set; }
    public DateRangeDto? ConsumptionPeriod { get; set; }
    public IReadOnlyList<ConsumptionImportIssueDto> Issues { get; set; } = Array.Empty<ConsumptionImportIssueDto>();
}
