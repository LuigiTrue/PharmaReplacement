public class ReplenishmentItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal MissingQuantity { get; set; }
    public ReplenishmentPriority Priority { get; set; }
    public ItemPriority ItemPriority { get; set; }
    public BatchStock? RecommendedBatch { get; set; } // Lote sugerido pelo FEFO
}