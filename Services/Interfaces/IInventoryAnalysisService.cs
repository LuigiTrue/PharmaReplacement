using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Interfaces;

/// <summary>
/// Contratos de análise gerencial e apresentação dos indicadores de estoque.
/// </summary>
public interface IInventoryAnalysisService
{
    /// <summary>
    /// Obtém indicadores gerais do conjunto de itens e do período consultados.
    /// </summary>
    Task<ItemsOverviewDto> GetItemsOverviewAsync(InventoryAnalysisQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os dados compostos necessários à análise individual de um item.
    /// </summary>
    Task<ItemAnalysisDto?> GetItemAnalysisAsync(int itemId, InventoryAnalysisQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a distribuição combinada ABC e XYZ de um conjunto de análise identificado.
    /// </summary>
    Task<AbcXyzMatrixDto> GetAbcXyzMatrixAsync(Guid abcAnalysisId, ItemFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta alertas paginados do comportamento de consumo.
    /// </summary>
    Task<PagedResultDto<ConsumptionAlertDto>> GetConsumptionAlertsAsync(InventoryAnalysisQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta desvios atípicos de consumo representados como alertas.
    /// </summary>
    Task<PagedResultDto<ConsumptionAlertDto>> GetAbnormalConsumptionAsync(InventoryAnalysisQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta itens que necessitam de maior acompanhamento e suas justificativas.
    /// </summary>
    Task<PagedResultDto<ItemPriorityDto>> GetHighPriorityItemsAsync(InventoryAnalysisQueryDto query, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém indicadores financeiros e disponibilidade de preços mensais do consumo.
    /// </summary>
    Task<FinancialConsumptionSummaryDto> GetFinancialConsumptionSummaryAsync(ConsumptionQueryDto query, CancellationToken cancellationToken = default);
}
