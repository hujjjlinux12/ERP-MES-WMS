public class PurchaseOrder
{
    public int Id {get;set;}
    public string SupplierCode {get;set;} = "";
    public string Status {get;set;} = "Draft"; // 订单状态，默认值为 "Draft"
    public List<PurchaseOrderLine> Lines {get;set;} = new();
}