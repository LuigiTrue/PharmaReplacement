namespace RepyPharma.Models;

public class Item
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    public ICollection<DailyConsumption> DailyConsumptions { get; set; } = new List<DailyConsumption>();
    public ReplenishmentRule? ReplenishmentRule { get; set; }
}
