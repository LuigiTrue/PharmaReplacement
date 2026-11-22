using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Interfaces;

/// <summary>
/// Contratos de consulta e indicadores de consumo.
/// </summary>
public interface IConsumptionAnalysisService
{
    /// <summary>
    /// Consulta o histórico diário paginado de consumo de um item.
    /// </summary>
    Task<PagedResultDto<DailyConsumptionDto>> GetDailyHistoryAsync(int itemId, DateRangeDto period, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o histórico mensal e o preço unitário de cada mês de um item.
    /// </summary>
    Task<PagedResultDto<MonthlyConsumptionDto>> GetMonthlyHistoryAsync(int itemId, DateRangeDto period, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a média de consumo de um item no período e granularidade informados.
    /// </summary>
    Task<AverageConsumptionDto> GetAverageConsumptionAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a variação percentual de consumo entre dois períodos.
    /// </summary>
    Task<ConsumptionVariationDto> GetConsumptionVariationAsync(int itemId, DateRangeDto referencePeriod, DateRangeDto comparisonPeriod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna os principais indicadores de consumo de um item no período.
    /// </summary>
    Task<ConsumptionSummaryDto> GetConsumptionSummaryAsync(int itemId, DateRangeDto period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a série de médias móveis para uma janela de observações.
    /// </summary>
    Task<MovingAverageDto> GetMovingAverageAsync(int itemId, DateRangeDto period, int windowSize, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compara séries de consumo de períodos equivalentes em anos distintos.
    /// </summary>
    Task<YearOverYearComparisonDto> GetYearOverYearComparisonAsync(int itemId, DateRangeDto referencePeriod, DateRangeDto comparisonPeriod, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna o ranking paginado de itens por volume de consumo.
    /// </summary>
    Task<PagedResultDto<ConsumptionRankingDto>> GetConsumptionRankingAsync(ConsumptionQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o consumo consolidado por grupo de produtos representado por ItemType.
    /// </summary>
    Task<IReadOnlyList<ConsumptionGroupDto>> GetConsumptionByGroupAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);
}
