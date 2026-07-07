using RepyPharma.Domain.Entities;

public class ProductStock
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public ItemType ItemType { get; set; }
    public decimal TotalStock { get; set; }
    public List<BatchStock> Batches { get; set; } = new();
}
