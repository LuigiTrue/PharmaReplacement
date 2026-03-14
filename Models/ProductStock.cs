public class ProductStock
{
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string Unit { get; set; } = "";

    public decimal CurrentStock { get; set; }

    public string Batch { get; set; } = "";

    public DateTime? Validity { get; set; }

    public decimal Quantity { get; set; }

    public string Manufacturer { get; set; } = "";
}