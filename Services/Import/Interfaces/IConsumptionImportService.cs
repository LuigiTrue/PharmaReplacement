using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Import.Interfaces;

public interface IConsumptionImportService
{
    /// <summary>Valida o CSV para o período exigido pelo card, sem gravar nem fechar o fluxo recebido.</summary>
    Task<ConsumptionImportValidationDto> ValidateImportAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default);
    /// <summary>Atualização parcial do mês corrente. Competências encerradas exigem o fluxo conjunto.</summary>
    Task<ConsumptionImportResultDto> ImportDailyConsumptionAsync(Stream csvStream, string fileName, ConsumptionImportOptionsDto? options = null, CancellationToken cancellationToken = default);
    Task<ConsumptionImportResultDto?> GetImportResultAsync(Guid importId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsumptionImportResultDto>> GetImportsAsync(CancellationToken cancellationToken = default);
    /// <summary>Reconstrói e concilia os dois documentos de um mês encerrado, sem persistência.</summary>
    Task<ConsumptionReportsValidationDto> PrepareCombinedImportAsync(Stream? dailyStream, string dailyFileName,
        Stream? sectorStream, string sectorFileName, DateOnly referenceMonth,
        IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default);
    /// <summary>Confirma o snapshot válido do circuito, revalidando cadastro/cobertura e gravando em uma transação.</summary>
    Task<CombinedConsumptionImportResultDto> ConfirmCombinedImportAsync(Guid validationId,
        IProgress<ConsumptionImportProgressDto>? progress = null, CancellationToken cancellationToken = default);
    void DiscardCombinedPreview(Guid validationId);
}
