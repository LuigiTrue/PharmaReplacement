using RepyPharma.Services.Import;

namespace RepyPharma.Services.Import.Interfaces;

public interface IConsumptionAverageReportService
{
    PdfImportValidationResult ValidateConsumptionReportPdf(string filePath);
    Task<ConsumptionAverageImportResult> ImportPdfAsync(string filePath);
}
