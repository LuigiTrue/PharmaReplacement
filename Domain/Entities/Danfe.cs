namespace RepyPharma.Domain.Entities;

public class Danfe
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string DanfeNumber { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public bool Boleto { get; set; }
    public string? UserReceiver { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public int QuotationId { get; set; }
    public bool IsThermolabile { get; set; }
    public int ItemsQuantity { get; set; }
    public bool ContainsPurchase { get; set; }
    public DateTime? PaymentDueAt { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public Quotation Quotation { get; set; } = null!;
}
