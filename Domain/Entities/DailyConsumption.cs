namespace RepyPharma.Domain.Entities;

public class DailyConsumption
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public DateTime ConsumptionDate { get; set; }
    public decimal Quantity { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Item Item { get; set; } = null!;
}
