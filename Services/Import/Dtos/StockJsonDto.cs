namespace RepyPharma.Services.Import.Dtos;

public class StockJsonDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TotalStock { get; set; }
    public List<BatchJsonDto> Batches { get; set; } = new();
}
