namespace RepyPharma.Domain.Entities;

public class Batch
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime Validity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Item Item { get; set; } = null!;
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}
