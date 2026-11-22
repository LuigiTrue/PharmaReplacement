using RepyPharma.Domain.Entities;

namespace RepyPharma.Models.Analytics;

/// <summary>
/// Unidade temporal utilizada na consulta de consumo.
/// </summary>
public enum ConsumptionGranularity
{
    Daily = 0,
    Monthly = 1
}

/// <summary>
/// Severidade informada por alertas e verificações de dados.
/// </summary>
public enum AnalysisSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Estado de uma futura operação de importação ou consolidação.
/// </summary>
public enum ConsumptionOperationStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// Intervalo inclusivo de datas, sem horário ou fuso.
/// </summary>
public class DateRangeDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

/// <summary>
/// Parâmetros de paginação; sua validação será implementada pelos serviços.
/// </summary>
public class PageRequestDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Página de resultados de transporte, sem expor entidades EF.
/// </summary>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
}

/// <summary>
/// Distingue dados disponíveis de informações sem fonte de persistência.
/// </summary>
public class DataAvailabilityDto<T> where T : class
{
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public T? Data { get; set; }
}

/// <summary>
/// Seleção de itens; o grupo atual de produtos corresponde a ItemType.
/// </summary>
public class ItemFilterDto
{
    public IReadOnlyList<int>? ItemIds { get; set; }
    public string? SearchText { get; set; }
    public ItemType? ItemType { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Identificação de um item para apresentação, sem navegação EF.
/// </summary>
public class ItemReferenceDto
{
    public int ItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public ItemType ItemType { get; set; }
}

/// <summary>
/// Intervalo, seleção de itens e granularidade de uma consulta coletiva.
/// </summary>
public class ConsumptionQueryDto
{
    public DateRangeDto Period { get; set; } = new();
    public ItemFilterDto Items { get; set; } = new();
    public ConsumptionGranularity Granularity { get; set; }
}

/// <summary>
/// Prioridade e justificativa de acompanhamento ou aquisição de um item.
/// </summary>
public class ItemPriorityDto
{
    public ItemReferenceDto Item { get; set; } = new();
    public string? PriorityCode { get; set; }
    public string? Reason { get; set; }
    public decimal? Score { get; set; }
}
