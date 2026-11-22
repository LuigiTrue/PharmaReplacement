using RepyPharma.Models.Analytics;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

/// <summary>
/// Contratos de consulta e manutenção manual da criticidade XYZ. Implementação incremental pendente.
/// </summary>
public class XyzCriticalityService : IXyzCriticalityService
{
    /// <summary>
    /// Consulta a criticidade assistencial e operacional atribuída manualmente ao item.
    /// </summary>
    public Task<ItemCriticalityDto?> GetItemCriticalityAsync(int itemId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta itens paginados por criticidade X, Y ou Z.
    /// </summary>
    public Task<PagedResultDto<ItemCriticalityDto>> GetItemsByCriticalityAsync(XyzCriticality criticality, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Atualiza manualmente a criticidade de um item mediante autorização do profissional.
    /// </summary>
    public Task<ItemCriticalityDto> UpdateItemCriticalityAsync(int itemId, UpdateItemCriticalityDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta itens sem classificação manual de criticidade.
    /// </summary>
    public Task<PagedResultDto<ItemCriticalityDto>> GetUnclassifiedItemsAsync(ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém a distribuição dos itens por criticidade e ausência de classificação.
    /// </summary>
    public Task<CriticalityDistributionDto> GetCriticalityDistributionAsync(ItemFilterDto filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta o histórico de criticidade somente quando houver fonte de auditoria disponível.
    /// </summary>
    public Task<DataAvailabilityDto<PagedResultDto<CriticalityHistoryEntryDto>>> GetCriticalityHistoryAsync(int itemId, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
