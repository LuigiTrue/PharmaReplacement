namespace RepyPharma.Domain.Entities;

public enum ReportImportStatus
{
    Processing = 0,
    Validated = 1,
    Completed = 2,
    ValidationFailed = 3,
    Failed = 4
}
