namespace RepyPharma.Services.Import.Interfaces;

public interface IJsonImportService
{
    Task<ImportResult> ImportStockAsync(string filePath);
    Task<ImportResult> ImportDailyConsumptionAsync(string filePath);
    Task<ImportResult> ImportAllAsync();
}
