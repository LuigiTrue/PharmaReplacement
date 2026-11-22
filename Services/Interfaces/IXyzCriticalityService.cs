using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Interfaces;

/// <summary>
/// Contratos de consulta e manutenção manual da criticidade XYZ.
/// </summary>
public interface IXyzCriticalityService
{
    /// <summary>
    /// Consulta a criticidade assistencial e operacional atribuída manualmente ao item.
    /// </summary>
    Task<ItemCriticalityDto?> GetItemCriticalityAsync(int itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta itens paginados por criticidade X, Y ou Z.
    /// </summary>
    Task<PagedResultDto<ItemCriticalityDto>> GetItemsByCriticalityAsync(XyzCriticality criticality, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza manualmente a criticidade de um item mediante autorização do profissional.
    /// </summary>
    Task<ItemCriticalityDto> UpdateItemCriticalityAsync(int itemId, UpdateItemCriticalityDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta itens sem classificação manual de criticidade.
    /// </summary>
    Task<PagedResultDto<ItemCriticalityDto>> GetUnclassifiedItemsAsync(ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a distribuição dos itens por criticidade e ausência de classificação.
    /// </summary>
    Task<CriticalityDistributionDto> GetCriticalityDistributionAsync(ItemFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o histórico de criticidade somente quando houver fonte de auditoria disponível.
    /// </summary>
    Task<DataAvailabilityDto<PagedResultDto<CriticalityHistoryEntryDto>>> GetCriticalityHistoryAsync(int itemId, PageRequestDto pagination, CancellationToken cancellationToken = default);
}
