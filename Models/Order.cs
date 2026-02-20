namespace RepyPharma.Models;

public class Order
{
    public int Id { get; set; }
    public string RemedyName { get; set; } = string.Empty;
    public decimal PercentageStock { get; set; }
    public decimal GrossValue { get; set; }
}
