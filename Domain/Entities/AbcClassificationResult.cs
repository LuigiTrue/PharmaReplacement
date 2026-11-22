namespace RepyPharma.Domain.Entities;

public class AbcClassificationResult
{
    public long Id { get; set; }
    public int ItemId { get; set; }

    /// <summary>
    /// Identifies one analysis and its item set. All results in that analysis
    /// must share this identifier and the same start and end dates.
    /// </summary>
    public Guid AnalysisId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal ConsumptionValue { get; set; }
    public decimal AccumulatedPercentage { get; set; }
    public AbcClassification Classification { get; set; }

    public Item Item { get; set; } = null!;
}
