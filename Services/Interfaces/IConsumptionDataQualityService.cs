using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Interfaces;

/// <summary>
/// Contratos de abrangência e integridade do histórico de consumo.
/// </summary>
public interface IConsumptionDataQualityService
{
    /// <summary>
    /// Obtém a abrangência temporal do histórico de consumo disponível.
    /// </summary>
    Task<ConsumptionDataCoverageDto> GetConsumptionDataCoverageAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifica períodos diários ou mensais sem registro de consumo para um item.
    /// </summary>
    Task<PagedResultDto<MissingConsumptionPeriodDto>> GetMissingConsumptionPeriodsAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta itens cujo histórico não atende ao mínimo de dias solicitado.
    /// </summary>
    Task<PagedResultDto<InsufficientHistoryItemDto>> GetItemsWithInsufficientHistoryAsync(ConsumptionQueryDto query, int minimumHistoryDays, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém indicadores gerais de qualidade e disponibilidade dos dados.
    /// </summary>
    Task<DataQualitySummaryDto> GetConsumptionDataQualitySummaryAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta informações da última importação quando houver metadados persistidos disponíveis.
    /// </summary>
    Task<DataAvailabilityDto<LastConsumptionImportDto>> GetLastImportInformationAsync(ItemFilterDto filter, CancellationToken cancellationToken = default);
}
