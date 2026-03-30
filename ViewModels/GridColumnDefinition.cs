using System.Linq.Expressions;
namespace RepyPharma.ViewModels;

public class GridColumnDefinition<TItem>
{
    public string Title { get; set; } = "";
    public bool Sortable { get; set; } = true;
    public Expression<Func<TItem, object>> Property { get; set; } = default!;
}