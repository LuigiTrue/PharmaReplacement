public class BatchStock
{
    public string Batch { get; set; } = "";
    public DateTime? Validity { get; set; }
    public List<StockLocation> Locations { get; set; } = new();
}