namespace RepyPharma.Domain.Entities;

public class ItemConsumptionAverage
{
    public int Id { get; set; }
    public int? ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    public DateTime? ReportGeneratedAt { get; set; }
    public int CoverageDays { get; set; }
    public string AveragePeriodKind { get; set; } = string.Empty;
    public decimal? MonthlyAverageOutput { get; set; }
    public decimal? WeeklyAverageOutput { get; set; }
    public decimal? CurrentAverageOutput { get; set; }
    public decimal? TotalOutput { get; set; }
    public decimal? StockBalance { get; set; }
    public decimal? ProjectedCoverageDays { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; }

    public Item? Item { get; set; }
}
