namespace RepyPharma.Models;

using RepyPharma.Domain.Entities;

public class FractionationSupplyData
{
    public int CoverageDays { get; set; }
    public List<FractionationSupplyItem> ReplenishmentItems { get; set; } = new();
    public List<FractionationSupplyItem> MinimumShortageItems { get; set; } = new();
}

public class FractionationSupplyItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public ItemType ItemType { get; set; }
    public decimal WeeklyAverageOutput { get; set; }
    public decimal DailyAverageOutput { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal CurrentFractionationStock { get; set; }
    public decimal CurrentPharmacyStock { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal SuggestedQuantity { get; set; }
    public DateTime? AverageReferenceStartDate { get; set; }
    public DateTime? AverageReferenceEndDate { get; set; }
    public string AveragePeriodKind { get; set; } = "";
    public BatchStock? RecommendedBatch { get; set; }
    public List<BatchStock> AvailableBatches { get; set; } = new();
}
