using System.Linq.Expressions;
using System.Reflection;
using RepyPharma.Components.Components.Models;

public interface IGridColumnService
{
    List<GridColumnDefinition<T>> Generate<T>(
        params Expression<Func<T, object>>[] properties);
}


public class GridColumnService : IGridColumnService
{
    public List<GridColumnDefinition<T>> Generate<T>(
        params Expression<Func<T, object>>[] properties)
    {
        var columns = new List<GridColumnDefinition<T>>();

        foreach (var expression in properties)
        {
            var propertyName = GetPropertyName(expression);

            columns.Add(new GridColumnDefinition<T>
            {
                Property = expression,
                Title = GenerateTitle(propertyName),
                Sortable = true
            });
        }

        return columns;
    }

    private static string GetPropertyName<T>(Expression<Func<T, object>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;

        if (expression.Body is UnaryExpression unary &&
            unary.Operand is MemberExpression unaryMember)
            return unaryMember.Member.Name;

        throw new InvalidOperationException("Expressão inválida.");
    }

    private static string GenerateTitle(string propertyName)
    {
        // Divide PascalCase automaticamente
        return System.Text.RegularExpressions.Regex
            .Replace(propertyName, "([a-z])([A-Z])", "$1 $2");
    }
}
