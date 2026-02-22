using System.Linq.Expressions;
namespace RepyPharma.Components.Components.Models;

public class GridColumnDefinition<T>
{
    public Expression<Func<T, object>> Property { get; set; } = default!;
    public string? Title { get; set; }
    public bool Sortable { get; set; } = true;
    public string? Format { get; set; }
}