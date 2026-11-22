namespace RepyPharma.Models.Analytics;

/// <summary>
/// Mês, ano e seleção de itens de uma futura consolidação.
/// </summary>
public class MonthlyAggregationRequestDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public ItemFilterDto Items { get; set; } = new();
}

/// <summary>
/// Identificação e andamento de uma futura consolidação mensal.
/// </summary>
public class ConsumptionAggregationStatusDto
{
    public Guid AggregationId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public ConsumptionOperationStatus Status { get; set; }
    public int ProcessedItemCount { get; set; }
    public int FailedItemCount { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public IReadOnlyList<string> Messages { get; set; } = Array.Empty<string>();
}
