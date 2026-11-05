namespace RepyPharma.Domain.Entities;

public class QuotationXItem
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public int ItemId { get; set; }
    public decimal QuantityRequested { get; set; }

    public Quotation Quotation { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
