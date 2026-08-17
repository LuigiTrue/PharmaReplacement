namespace RepyPharma.Domain.Entities;

public class ReportImport
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public short ReferenceYear { get; set; }
    public short ReferenceMonth { get; set; }
    public ReportImportStatus Status { get; set; } = ReportImportStatus.Processing;
    public string? SourceFileName { get; set; }
    public string? FileHash { get; set; }
    public DateTimeOffset? ImportedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public int? TotalItems { get; set; }
    public int? ValidItems { get; set; }
    public int? InvalidItems { get; set; }
    public string? ErrorMessage { get; set; }

    public Location Location { get; set; } = null!;
    public ICollection<ReportItem> Items { get; set; } = new List<ReportItem>();
}
