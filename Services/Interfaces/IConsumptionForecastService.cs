using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Interfaces;

/// <summary>
/// Contratos de projeção e avaliação do comportamento da demanda.
/// </summary>
public interface IConsumptionForecastService
{
    /// <summary>
    /// Obtém a projeção de consumo de um item pelo método e horizonte solicitados.
    /// </summary>
    Task<ConsumptionForecastDto> GetForecastAsync(int itemId, ForecastRequestDto request, ForecastMethodOptionsDto method, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compara projeções de métodos identificados sem definir algoritmos nesta etapa.
    /// </summary>
    Task<ForecastComparisonDto> CompareForecastMethodsAsync(int itemId, ForecastRequestDto request, IReadOnlyList<ForecastMethodOptionsDto> methods, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém indicadores de tendência de crescimento ou redução do consumo.
    /// </summary>
    Task<ConsumptionTrendDto> GetConsumptionTrendAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém indicadores de sazonalidade do histórico de consumo.
    /// </summary>
    Task<SeasonalityAnalysisDto> GetSeasonalityAnalysisAsync(int itemId, DateRangeDto period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Avalia a precisão histórica dos métodos quando existirem dados de projeções disponíveis.
    /// </summary>
    Task<DataAvailabilityDto<ForecastAccuracyDto>> GetHistoricalForecastAccuracyAsync(int itemId, DateRangeDto evaluationPeriod, IReadOnlyList<string> methodCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém indicadores de variabilidade da demanda de um item.
    /// </summary>
    Task<DemandVariabilityDto> GetDemandVariabilityAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém indicadores de demanda intermitente para um item.
    /// </summary>
    Task<IntermittentDemandDto> GetIntermittentDemandAnalysisAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default);
}
