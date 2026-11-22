namespace RepyPharma.Domain.Entities;

public class MonthlyConsumption
{
    public long Id { get; set; }
    public int ItemId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AverageDailyConsumption { get; set; }

    /// <summary>
    /// Unit purchase price for the reference year and month; null when unknown.
    /// </summary>
    public decimal? UnitPurchasePrice { get; set; }

    public Item Item { get; set; } = null!;
}
