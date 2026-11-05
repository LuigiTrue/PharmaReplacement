namespace RepyPharma.Domain.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? Observations { get; set; }

    public ICollection<Danfe> Danfes { get; set; } = new List<Danfe>();
}
