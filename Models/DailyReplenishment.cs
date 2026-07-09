namespace RepyPharma.Models;

using RepyPharma.Domain.Entities;

public class DailyReplenishmentData
{
    public int CoverageDays { get; set; }
    public DateTime? LastAverageImportedAt { get; set; }
    public List<DailyReplenishmentItem> Materials { get; set; } = new();
    public List<DailyReplenishmentItem> Medications { get; set; } = new();
    public List<DailyReplenishmentItem> ZeroStockItems { get; set; } = new();
    public int TotalRequestItems => Materials.Count + Medications.Count;
}

public class DailyReplenishmentItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public ItemType ItemType { get; set; }
    public decimal AverageOutput { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ProjectionDays { get; set; }
    public decimal SuggestedQuantity { get; set; }
    public bool IsMaterial => ItemType == ItemType.Material;
}
