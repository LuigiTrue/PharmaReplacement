using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Interfaces;

/// <summary>
/// Contratos de necessidade, prioridade e indicadores de planejamento de compras.
/// </summary>
public interface IPurchasePlanningService
{
    /// <summary>
    /// Obtém as projeções paginadas de aquisição para o horizonte e conjunto solicitados.
    /// </summary>
    Task<PagedResultDto<PurchasePlanningDto>> GetPurchaseProjectionAsync(ItemFilterDto filter, PurchasePlanningParametersDto parameters, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a necessidade de aquisição de um item considerando os parâmetros informados.
    /// </summary>
    Task<PurchaseRequirementDto> CalculatePurchaseRequirementAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a prioridade e a justificativa de aquisição de um item.
    /// </summary>
    Task<ItemPriorityDto> GetPurchasePriorityAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os dados de autonomia estimada do estoque de um item.
    /// </summary>
    Task<StockCoverageDto> GetStockCoverageAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os parâmetros e o resultado do futuro cálculo de ponto de reposição.
    /// </summary>
    Task<ReorderPointDto> GetReorderPointAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os parâmetros e o resultado do futuro cálculo de estoque de segurança.
    /// </summary>
    Task<SafetyStockDto> GetSafetyStockAsync(int itemId, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o resumo de planejamento de compras do conjunto de itens solicitado.
    /// </summary>
    Task<PurchasePlanningSummaryDto> GetPurchasePlanningSummaryAsync(ItemFilterDto filter, PurchasePlanningParametersDto parameters, CancellationToken cancellationToken = default);
}
