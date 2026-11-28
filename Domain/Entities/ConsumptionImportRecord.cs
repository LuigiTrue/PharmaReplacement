namespace RepyPharma.Domain.Entities;

/// <summary>Histórico de gravações de CSV de consumo concluídas com sucesso.</summary>
public class ConsumptionImportRecord
{
    public Guid Id { get; set; }
    public DateOnly ReferenceMonth { get; set; }
    public DateOnly RecordedThrough { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public DateTimeOffset ImportedAt { get; set; }
}
