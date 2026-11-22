using RepyPharma.Models.Analytics;

namespace RepyPharma.Services.Interfaces;

/// <summary>
/// Contratos de classificação ABC por período e conjunto de itens.
/// </summary>
public interface IAbcClassificationService
{
    /// <summary>
    /// Calcula a classificação ABC para o período e conjunto de itens selecionados.
    /// </summary>
    Task<AbcAnalysisDto> CalculateAbcClassificationAsync(AbcAnalysisRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta a classificação atual de um item no contexto de uma análise ou conjunto identificado.
    /// </summary>
    Task<AbcClassificationDto?> GetCurrentClassificationAsync(int itemId, AbcClassificationScopeDto scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o histórico paginado de classificações ABC de um item.
    /// </summary>
    Task<PagedResultDto<AbcClassificationDto>> GetClassificationHistoryAsync(int itemId, AbcClassificationScopeDto scope, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta o ranking financeiro paginado de uma análise ABC identificada.
    /// </summary>
    Task<PagedResultDto<AbcClassificationDto>> GetClassificationRankingAsync(Guid analysisId, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém a distribuição de classes do conjunto completo de uma análise ABC.
    /// </summary>
    Task<AbcDistributionDto> GetAbcDistributionAsync(Guid analysisId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém dados paginados para o Pareto, mantendo a referência ao conjunto completo da análise.
    /// </summary>
    Task<ParetoAnalysisDto> GetParetoAnalysisAsync(Guid analysisId, PageRequestDto pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifica mudanças de classificação de itens entre duas análises ABC.
    /// </summary>
    Task<PagedResultDto<AbcClassificationChangeDto>> GetAbcClassificationChangesAsync(Guid referenceAnalysisId, Guid comparisonAnalysisId, ItemFilterDto filter, PageRequestDto pagination, CancellationToken cancellationToken = default);
}
