using RepyPharma.Services.Import;

namespace RepyPharma.Services.Import.Interfaces;

public interface IConsumptionAverageReportService
{
    Task<ConsumptionAverageImportResult> ImportPdfAsync(string filePath);
}
