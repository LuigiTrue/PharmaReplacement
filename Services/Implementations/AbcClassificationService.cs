using RepyPharma.Models.Analytics;
using RepyPharma.Services.Interfaces;

namespace RepyPharma.Services.Implementatios;

/// <summary>
/// Contratos de classificação ABC por período e conjunto de itens. Implementação incremental pendente.
/// </summary>
public class AbcClassificationService : IAbcClassificationService
{
    /// <summary>
    /// Calcula a classificação ABC para o período e conjunto de itens selecionados.
    /// </summary>
    public Task<AbcAnalysisDto> CalculateAbcClassificationAsync(AbcAnalysisRequestDto request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta a classificação atual de um item no contexto de uma análise ou conjunto identificado.
    /// </summary>
    public Task<AbcClassificationDto?> GetCurrentClassificationAsync(int itemId, AbcClassificationScopeDto scope, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta o histórico paginado de classificações ABC de um item.
    /// </summary>
    public Task<PagedResultDto<AbcClassificationDto>> GetClassificationHistoryAsync(int itemId, AbcClassificationScopeDto scope, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Consulta o ranking financeiro paginado de uma análise ABC identificada.
    /// </summary>
    public Task<PagedResultDto<AbcClassificationDto>> GetClassificationRankingAsync(Guid analysisId, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém a distribuição de classes do conjunto completo de uma análise ABC.
    /// </summary>
    public Task<AbcDistributionDto> GetAbcDistributionAsync(Guid analysisId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Obtém dados paginados para o Pareto, mantendo a referência ao conjunto completo da análise.
    /// </summary>
    public Task<ParetoAnalysisDto> GetParetoAnalysisAsync(Guid analysisId, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Identifica mudanças de classificação de itens entre duas análises ABC.
    /// </summary>
    public Task<PagedResultDto<AbcClassificationChangeDto>> GetAbcClassificationChangesAsync(Guid referenceAnalysisId, Guid comparisonAnalysisId, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
