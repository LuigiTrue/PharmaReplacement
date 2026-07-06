namespace RepyPharma.Domain.Entities;

public class StockBalance
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int BatchId { get; set; }
    public int LocationId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Item Item { get; set; } = null!;
    public Batch Batch { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
