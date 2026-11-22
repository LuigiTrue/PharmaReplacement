using RepyPharma.Models.Analytics;
using RepyPharma.Services.Import.Interfaces;

namespace RepyPharma.Services.Import;

/// <summary>
/// Contratos de consolidação e recálculo futuro do consumo mensal. Implementação incremental pendente.
/// </summary>
public class ConsumptionAggregationService : IConsumptionAggregationService
{
    /// <summary>
    /// Consolida futuramente o consumo diário do mês e do conjunto de itens solicitado.
    /// </summary>
    public Task<ConsumptionAggregationStatusDto> ConsolidateMonthlyConsumptionAsync(MonthlyAggregationRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Recalcula futuramente a consolidação mensal para o recorte solicitado.
    /// </summary>
    public Task<ConsumptionAggregationStatusDto> RecalculateMonthlyConsumptionAsync(MonthlyAggregationRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta o andamento de uma operação de consolidação identificada.
    /// </summary>
    public Task<ConsumptionAggregationStatusDto?> GetAggregationStatusAsync(Guid aggregationId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
