namespace RepyPharma.Services.Import;

public sealed class ConsumptionReportsImportOptions
{
    public int PreviewLifetimeMinutes { get; set; } = 15;
    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024;
}
