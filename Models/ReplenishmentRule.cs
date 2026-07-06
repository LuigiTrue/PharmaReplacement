namespace RepyPharma.Models;

public class ReplenishmentRule
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public decimal? MinimumStock { get; set; }
    public decimal? SafetyStock { get; set; }
    public int? LeadTimeDays { get; set; }
    public int? TargetCoverageDays { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }

    public Item Item { get; set; } = null!;
}
