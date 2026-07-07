namespace RepyPharma.Services.Import;

public class ConsumptionAverageImportResult
{
    public int ParsedItems { get; set; }
    public int CreatedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int MissingItems { get; set; }
    public int Errors { get; set; }
    public DateTime? ReportStartDate { get; set; }
    public DateTime? ReportEndDate { get; set; }
    public DateTime? ReportGeneratedAt { get; set; }
    public int CoverageDays { get; set; }
    public string AveragePeriodKind { get; set; } = string.Empty;
    public List<string> ErrorMessages { get; set; } = new();

    public void AddError(string message)
    {
        Errors++;
        ErrorMessages.Add(message);
    }
}
