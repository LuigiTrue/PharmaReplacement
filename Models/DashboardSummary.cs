namespace RepyPharma.Models;

public class DashboardSummary
{
    public List<ReplenishmentItem> NeedToBuy { get; set; } = new();       // Critical
    public List<ReplenishmentItem> RunningLow { get; set; } = new();      // Warning
    public List<ReplenishmentItem> AboveNormal { get; set; } = new();     // Excesso
}