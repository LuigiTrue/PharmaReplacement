namespace RepyPharma.Domain.Entities;

public class ReportItem
{
    public int Id { get; set; }
    public int ReportImportId { get; set; }
    public int ItemId { get; set; }
    public decimal TotalOutput { get; set; }
    public decimal? AverageDailyOutput { get; set; }
    public short? MovementDays { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? AverageUnitCost { get; set; }

    public ReportImport ReportImport { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
