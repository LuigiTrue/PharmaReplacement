namespace RepyPharma.Domain.Entities;

public class DailyConsumption
{
    public long Id { get; set; }
    public int ItemId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Quantity { get; set; }

    public Item Item { get; set; } = null!;
}
