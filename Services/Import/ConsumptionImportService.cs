using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import.Interfaces;

namespace RepyPharma.Services.Import;

/// <summary>
/// Contratos de validação e importação futura de consumo diário em CSV. Implementação incremental pendente.
/// </summary>
public class ConsumptionImportService : IConsumptionImportService
{
    /// <summary>
    /// Valida a estrutura e os dados de um CSV; a gestão do fluxo permanece com o chamador.
    /// </summary>
    public Task<ConsumptionImportValidationDto> ValidateImportAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Importa futuramente os registros diários do CSV, sem implementar leitura ou persistência nesta etapa.
    /// </summary>
    public Task<ConsumptionImportResultDto> ImportDailyConsumptionAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta o resultado de uma importação de consumo identificada.
    /// </summary>
    public Task<ConsumptionImportResultDto?> GetImportResultAsync(Guid importId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
