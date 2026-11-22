namespace RepyPharma.Domain.Entities;

public class Location
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}
