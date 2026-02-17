namespace RepyPharma.Models;

public class Order
{
    public int Id { get; set; }
    public string Country { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public decimal GrossValue { get; set; }
}
