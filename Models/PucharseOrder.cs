namespace RepyPharma.Models;

public class PurchaseOrder
{
    public int PurchaseId {get;set;}
    public List<ProductStock> PurchaseItens {get;set;}
    public List<PurchaseItemQuantityOrder> OrderQuantityValue {get;set;}
}

public class PurchaseItemQuantityOrder {
    public int ItemId {get;set;}
    public int QuantityNeeded {get;set;} 
}