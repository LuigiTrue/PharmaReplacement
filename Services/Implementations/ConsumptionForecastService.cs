using RepyPharma.Models.Analytics;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

/// <summary>
/// Contratos de projeção e avaliação do comportamento da demanda. Implementação incremental pendente.
/// </summary>
public class ConsumptionForecastService : IConsumptionForecastService
{
    /// <summary>
    /// Obtém a projeção de consumo de um item pelo método e horizonte solicitados.
    /// </summary>
    public Task<ConsumptionForecastDto> GetForecastAsync(int itemId, ForecastRequestDto request, ForecastMethodOptionsDto method, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Compara projeções de métodos identificados sem definir algoritmos nesta etapa.
    /// </summary>
    public Task<ForecastComparisonDto> CompareForecastMethodsAsync(int itemId, ForecastRequestDto request, IReadOnlyList<ForecastMethodOptionsDto> methods, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém indicadores de tendência de crescimento ou redução do consumo.
    /// </summary>
    public Task<ConsumptionTrendDto> GetConsumptionTrendAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém indicadores de sazonalidade do histórico de consumo.
    /// </summary>
    public Task<SeasonalityAnalysisDto> GetSeasonalityAnalysisAsync(int itemId, DateRangeDto period, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Avalia a precisão histórica dos métodos quando existirem dados de projeções disponíveis.
    /// </summary>
    public Task<DataAvailabilityDto<ForecastAccuracyDto>> GetHistoricalForecastAccuracyAsync(int itemId, DateRangeDto evaluationPeriod, IReadOnlyList<string> methodCodes, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém indicadores de variabilidade da demanda de um item.
    /// </summary>
    public Task<DemandVariabilityDto> GetDemandVariabilityAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém indicadores de demanda intermitente para um item.
    /// </summary>
    public Task<IntermittentDemandDto> GetIntermittentDemandAnalysisAsync(int itemId, DateRangeDto period, ConsumptionGranularity granularity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
