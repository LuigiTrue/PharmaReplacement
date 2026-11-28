using RepyPharma.Models.Analytics;
namespace RepyPharma.Services.Import.Interfaces;

public interface ISectorConsumptionImportService
{
    Task<SectorConsumptionReportDto> ProcessSectorConsumptionCsvAsync(Stream csvStream, string fileName,
        IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default);
}
