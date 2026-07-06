namespace RepyPharma.Services.Import.Dtos;

public class DailyConsumptionJsonDto
{
    public string Code { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public DateTime? ConsumptionDate { get; set; }
    public DateTime? Date { get; set; }
    public decimal Quantity { get; set; }
    public string Source { get; set; } = string.Empty;
}
