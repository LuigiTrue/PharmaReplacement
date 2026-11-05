namespace RepyPharma.Domain.Entities;

public class Quotation
{
    public int Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;

    public ICollection<QuotationXItem> Items { get; set; } = new List<QuotationXItem>();
    public ICollection<Danfe> Danfes { get; set; } = new List<Danfe>();
}
