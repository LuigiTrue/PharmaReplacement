namespace RepyPharma.Models.Analytics;

/// <summary>
/// Direção de tendência; critérios e algoritmos serão definidos posteriormente.
/// </summary>
public enum ConsumptionTrendDirection
{
    Stable = 0,
    Increasing = 1,
    Decreasing = 2
}

/// <summary>
/// Identificador extensível e parâmetros numéricos de um método ainda a implementar.
/// </summary>
public class ForecastMethodOptionsDto
{
    public string MethodCode { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, decimal> Parameters { get; set; } = new Dictionary<string, decimal>();
}

/// <summary>
/// Histórico, início futuro e horizonte em dias de uma projeção.
/// </summary>
public class ForecastRequestDto
{
    public DateRangeDto HistoryPeriod { get; set; } = new();
    public DateOnly ForecastStartDate { get; set; }
    public int HorizonDays { get; set; }
    public ConsumptionGranularity Granularity { get; set; }
}

/// <summary>
/// Quantidade projetada e limites opcionais para uma data.
/// </summary>
public class ForecastPointDto
{
    public DateOnly Date { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
}

/// <summary>
/// Série projetada com identificação do método, histórico e intervalo futuro.
/// </summary>
public class ConsumptionForecastDto
{
    public int ItemId { get; set; }
    public ForecastMethodOptionsDto Method { get; set; } = new();
    public DateRangeDto HistoryPeriod { get; set; } = new();
    public DateRangeDto ForecastPeriod { get; set; } = new();
    public decimal? TotalQuantity { get; set; }
    public IReadOnlyList<ForecastPointDto> Points { get; set; } = Array.Empty<ForecastPointDto>();
}

/// <summary>
/// Projeções alternativas para comparação, sem pressupor um algoritmo vencedor.
/// </summary>
public class ForecastComparisonDto
{
    public int ItemId { get; set; }
    public IReadOnlyList<ConsumptionForecastDto> Forecasts { get; set; } = Array.Empty<ConsumptionForecastDto>();
}

/// <summary>
/// Indicadores de direção e intensidade da tendência de consumo.
/// </summary>
public class ConsumptionTrendDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public ConsumptionTrendDirection? Direction { get; set; }
    public decimal? Slope { get; set; }
    public decimal? VariationPercentage { get; set; }
    public IReadOnlyList<ConsumptionSeriesPointDto> Series { get; set; } = Array.Empty<ConsumptionSeriesPointDto>();
}

/// <summary>
/// Indicadores sazonais para um mês do ano.
/// </summary>
public class SeasonalityPointDto
{
    public int Month { get; set; }
    public decimal? SeasonalityIndex { get; set; }
    public decimal? AverageConsumption { get; set; }
}

/// <summary>
/// Indicadores de sazonalidade de um item no histórico selecionado.
/// </summary>
public class SeasonalityAnalysisDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public bool? HasSeasonality { get; set; }
    public IReadOnlyList<SeasonalityPointDto> Points { get; set; } = Array.Empty<SeasonalityPointDto>();
}

/// <summary>
/// Valor de uma métrica histórica identificada por método e código da métrica.
/// </summary>
public class ForecastAccuracyMetricDto
{
    public string MethodCode { get; set; } = string.Empty;
    public string MetricCode { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}

/// <summary>
/// Métricas de precisão histórica, quando houver uma fonte disponível.
/// </summary>
public class ForecastAccuracyDto
{
    public int ItemId { get; set; }
    public DateRangeDto EvaluationPeriod { get; set; } = new();
    public IReadOnlyList<ForecastAccuracyMetricDto> Metrics { get; set; } = Array.Empty<ForecastAccuracyMetricDto>();
}

/// <summary>
/// Indicadores estatísticos de variabilidade, sem definição de seu cálculo nesta etapa.
/// </summary>
public class DemandVariabilityDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public int ObservationCount { get; set; }
    public decimal? AverageConsumption { get; set; }
    public decimal? StandardDeviation { get; set; }
    public decimal? CoefficientOfVariation { get; set; }
}

/// <summary>
/// Indicadores de ocorrência e intervalos de demanda intermitente.
/// </summary>
public class IntermittentDemandDto
{
    public int ItemId { get; set; }
    public DateRangeDto Period { get; set; } = new();
    public int ObservationCount { get; set; }
    public int PeriodsWithoutConsumption { get; set; }
    public decimal? AverageDemandInterval { get; set; }
    public decimal? SquaredCoefficientOfVariation { get; set; }
    public bool? IsIntermittent { get; set; }
}
