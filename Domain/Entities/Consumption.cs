namespace RepyPharma.Domain.Entities;

public class Consumption
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public DateTime InitialDate { get; set; }
    public DateTime FinalDate { get; set; }
    public decimal TotalConsumption { get; set; }
    public decimal DailyAverage { get; set; }
    public decimal? AverageVariation { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

    public Item Item { get; set; } = null!;
}
