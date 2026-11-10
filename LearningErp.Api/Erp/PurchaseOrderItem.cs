public class PurchaseOrderLine
{
    public int Id {get;set;}
    public int PurchaseOrderId {get;set;} 
    public string MaterialCode {get;set;}= "";
    public int Quantity {get;set;}
}