using RepyPharma.Models.Analytics;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

/// <summary>
/// Contratos de consulta e indicadores de consumo. Implementação incremental pendente.
/// </summary>
public class ConsumptionAnalysisService : IConsumptionAnalysisService
{
    /// <summary>
    /// Consulta o histórico diário paginado de consumo de um item.
    /// </summary>
    public Task<PagedResultDto<DailyConsumptionDto>> GetDailyHistoryAsync(int itemId, DateRangeDto period, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta o histórico mensal e o preço unitário de cada mês de um item.
    /// </summary>
    public Task<PagedResultDto<MonthlyConsumptionDto>> GetMonthlyHistoryAsync(int itemId, DateRangeDto period, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém a média de consumo de um item no período e granularidade informados.
    /// </summary>
    public Task<AverageConsumptionDto> GetAverageConsumptionAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém a variação percentual de consumo entre dois períodos.
    /// </summary>
    public Task<ConsumptionVariationDto> GetConsumptionVariationAsync(int itemId, DateRangeDto referencePeriod, DateRangeDto comparisonPeriod, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retorna os principais indicadores de consumo de um item no período.
    /// </summary>
    public Task<ConsumptionSummaryDto> GetConsumptionSummaryAsync(int itemId, DateRangeDto period, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém a série de médias móveis para uma janela de observações.
    /// </summary>
    public Task<MovingAverageDto> GetMovingAverageAsync(int itemId, DateRangeDto period, int windowSize, ConsumptionGranularity granularity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compara séries de consumo de períodos equivalentes em anos distintos.
    /// </summary>
    public Task<YearOverYearComparisonDto> GetYearOverYearComparisonAsync(int itemId, DateRangeDto referencePeriod, DateRangeDto comparisonPeriod, ConsumptionGranularity granularity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retorna o ranking paginado de itens por volume de consumo.
    /// </summary>
    public Task<PagedResultDto<ConsumptionRankingDto>> GetConsumptionRankingAsync(ConsumptionQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém o consumo consolidado por grupo de produtos representado por ItemType.
    /// </summary>
    public Task<IReadOnlyList<ConsumptionGroupDto>> GetConsumptionByGroupAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
