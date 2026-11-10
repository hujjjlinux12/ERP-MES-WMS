public class CreatePurchaseOrderRequest
{
    public string SupplierCode {get;set;} = "";
    public List<CreatePurchaseOrderLineRequest> Lines {get;set;} = new();
}

public class CreatePurchaseOrderLineRequest
{
    public string MaterialCode {get;set;}= "";
    public int Quantity {get;set;}
}