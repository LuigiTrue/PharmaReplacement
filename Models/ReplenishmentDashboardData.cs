namespace RepyPharma.Models;

public class ReplenishmentDashboardData
{
    public string LocationId { get; set; } = "";
    public string LocationName { get; set; } = "";
    public decimal ReplenishmentCompletionPercentage { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal CoveredQuantity { get; set; }
    public decimal MissingQuantity { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int BelowMinimumItems { get; set; }
    public List<ReplenishmentDashboardItem> Items { get; set; } = new();
    public List<ReplenishmentDashboardChartPoint> CompletionChart { get; set; } = new();
    public List<ReplenishmentDashboardChartPoint> MissingByItemChart { get; set; } = new();
    public List<ReplenishmentDashboardChartPoint> TopReplenishmentItemsChart { get; set; } = new();
}

public class ReplenishmentDashboardItem
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal CoveredQuantity { get; set; }
    public decimal MissingQuantity { get; set; }
    public decimal CompletionPercentage { get; set; }
    public int SupplyPriorityRank { get; set; }
    public string SupplyPriorityGroup { get; set; } = "";
    public bool IsBelowMinimum => MissingQuantity > 0;
}

public class ReplenishmentDashboardChartPoint
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
}
