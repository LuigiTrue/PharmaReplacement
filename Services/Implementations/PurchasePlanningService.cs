using RepyPharma.Models.Analytics;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

/// <summary>
/// Contratos de necessidade, prioridade e indicadores de planejamento de compras. Implementação incremental pendente.
/// </summary>
public class PurchasePlanningService : IPurchasePlanningService
{
    /// <summary>
    /// Obtém as projeções paginadas de aquisição para o horizonte e conjunto solicitados.
    /// </summary>
    public Task<PagedResultDto<PurchasePlanningDto>> GetPurchaseProjectionAsync(ItemFilterDto filter, PurchasePlanningParametersDto parameters, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém a necessidade de aquisição de um item considerando os parâmetros informados.
    /// </summary>
    public Task<PurchaseRequirementDto> CalculatePurchaseRequirementAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém a prioridade e a justificativa de aquisição de um item.
    /// </summary>
    public Task<ItemPriorityDto> GetPurchasePriorityAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém os dados de autonomia estimada do estoque de um item.
    /// </summary>
    public Task<StockCoverageDto> GetStockCoverageAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém os parâmetros e o resultado do futuro cálculo de ponto de reposição.
    /// </summary>
    public Task<ReorderPointDto> GetReorderPointAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém os parâmetros e o resultado do futuro cálculo de estoque de segurança.
    /// </summary>
    public Task<SafetyStockDto> GetSafetyStockAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém o resumo de planejamento de compras do conjunto de itens solicitado.
    /// </summary>
    public Task<PurchasePlanningSummaryDto> GetPurchasePlanningSummaryAsync(ItemFilterDto filter, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
