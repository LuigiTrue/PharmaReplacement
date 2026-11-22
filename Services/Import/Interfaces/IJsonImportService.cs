namespace RepyPharma.Services.Import.Interfaces;

public interface IJsonImportService
{
    Task<ImportResult> ImportStockAsync(string filePath);
}
