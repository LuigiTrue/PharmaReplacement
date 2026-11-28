namespace RepyPharma.Models.Analytics;

/// <summary>A competência é definida pelo card; o CSV Soul MV não informa mês/ano.</summary>
public class ConsumptionImportOptionsDto
{
    public DateOnly? ReferenceMonth { get; set; }
}

public class ConsumptionImportIssueDto
{
    public long? RowNumber { get; set; }
    public string? ColumnName { get; set; }
    public string? ItemCode { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AnalysisSeverity Severity { get; set; }
}

public class ConsumptionImportValidationDto
{
    public bool IsValid { get; set; }
    public DateRangeDto? DetectedPeriod { get; set; }
    public IReadOnlyList<ConsumptionImportIssueDto> Issues { get; set; } = Array.Empty<ConsumptionImportIssueDto>();
}

public class ConsumptionImportResultDto
{
    public Guid ImportId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public ConsumptionOperationStatus Status { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateRangeDto? ConsumptionPeriod { get; set; }
    public IReadOnlyList<ConsumptionImportIssueDto> Issues { get; set; } = Array.Empty<ConsumptionImportIssueDto>();
}
