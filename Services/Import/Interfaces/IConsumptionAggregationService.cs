using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Import.Interfaces;

/// <summary>
/// Contratos de consolidação e recálculo futuro do consumo mensal.
/// </summary>
public interface IConsumptionAggregationService
{
    /// <summary>
    /// Consolida futuramente o consumo diário do mês e do conjunto de itens solicitado.
    /// </summary>
    Task<ConsumptionAggregationStatusDto> ConsolidateMonthlyConsumptionAsync(MonthlyAggregationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalcula futuramente a consolidação mensal para o recorte solicitado.
    /// </summary>
    Task<ConsumptionAggregationStatusDto> RecalculateMonthlyConsumptionAsync(MonthlyAggregationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o andamento de uma operação de consolidação identificada.
    /// </summary>
    Task<ConsumptionAggregationStatusDto?> GetAggregationStatusAsync(Guid aggregationId, CancellationToken cancellationToken = default);
}
