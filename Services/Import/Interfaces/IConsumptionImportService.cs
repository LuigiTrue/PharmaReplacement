using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Import.Interfaces;

/// <summary>
/// Contratos de validação e importação futura de consumo diário em CSV.
/// </summary>
public interface IConsumptionImportService
{
    /// <summary>
    /// Valida a estrutura e os dados de um CSV; a gestão do fluxo permanece com o chamador.
    /// </summary>
    Task<ConsumptionImportValidationDto> ValidateImportAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Importa futuramente os registros diários do CSV, sem implementar leitura ou persistência nesta etapa.
    /// </summary>
    Task<ConsumptionImportResultDto> ImportDailyConsumptionAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o resultado de uma importação de consumo identificada.
    /// </summary>
    Task<ConsumptionImportResultDto?> GetImportResultAsync(Guid importId, CancellationToken cancellationToken = default);
}
