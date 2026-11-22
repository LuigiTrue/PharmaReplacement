namespace RepyPharma.Models.Analytics;

/// <summary>
/// Criticidade assistencial e operacional atribuída manualmente; não representa variabilidade de consumo.
/// </summary>
public enum XyzCriticality
{
    X = 0,
    Y = 1,
    Z = 2
}

/// <summary>
/// Criticidade atual de um item; classificação nula indica ausência de classificação.
/// </summary>
public class ItemCriticalityDto
{
    public ItemReferenceDto Item { get; set; } = new();
    public XyzCriticality? Criticality { get; set; }
}

/// <summary>
/// Solicitação de alteração manual da criticidade por profissional autorizado.
/// </summary>
public class UpdateItemCriticalityDto
{
    public XyzCriticality? Criticality { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Registro de transporte para histórico, caso uma fonte de auditoria seja disponibilizada.
/// </summary>
public class CriticalityHistoryEntryDto
{
    public int ItemId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public XyzCriticality? PreviousCriticality { get; set; }
    public XyzCriticality? CurrentCriticality { get; set; }
    public string? ChangedByUserId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Contagem de itens por criticidade, incluindo os não classificados.
/// </summary>
public class CriticalityDistributionDto
{
    public int XCount { get; set; }
    public int YCount { get; set; }
    public int ZCount { get; set; }
    public int UnclassifiedCount { get; set; }
}
