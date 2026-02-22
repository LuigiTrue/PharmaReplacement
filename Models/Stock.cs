namespace RepyPharma.Models;

public class Stock
{
    public int Id { get; set; }
    public string StockName { get; set; } = string.Empty;
    public decimal DangerLevel { get; set; }
    public int StockCode { get; set; }
    public string TestText { get; set; } = string.Empty;
}
