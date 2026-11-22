using RepyPharma.Models.Analytics;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

/// <summary>
/// Contratos de abrangência e integridade do histórico de consumo. Implementação incremental pendente.
/// </summary>
public class ConsumptionDataQualityService : IConsumptionDataQualityService
{
    /// <summary>
    /// Obtém a abrangência temporal do histórico de consumo disponível.
    /// </summary>
    public Task<ConsumptionDataCoverageDto> GetConsumptionDataCoverageAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Identifica períodos diários ou mensais sem registro de consumo para um item.
    /// </summary>
    public Task<PagedResultDto<MissingConsumptionPeriodDto>> GetMissingConsumptionPeriodsAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta itens cujo histórico não atende ao mínimo de dias solicitado.
    /// </summary>
    public Task<PagedResultDto<InsufficientHistoryItemDto>> GetItemsWithInsufficientHistoryAsync(ConsumptionQueryDto query, int minimumHistoryDays, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém indicadores gerais de qualidade e disponibilidade dos dados.
    /// </summary>
    public Task<DataQualitySummaryDto> GetConsumptionDataQualitySummaryAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta informações da última importação quando houver metadados persistidos disponíveis.
    /// </summary>
    public Task<DataAvailabilityDto<LastConsumptionImportDto>> GetLastImportInformationAsync(ItemFilterDto filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
