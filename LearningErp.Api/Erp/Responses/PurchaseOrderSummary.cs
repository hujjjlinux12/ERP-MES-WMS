// record 类型会自动生成4个属性和一个构造方法。它是采购列表的响应DTO，用来规定前端会收到哪些字段
public record PurchaseOrderSummary(int Id , string SupplierCode , string Status , int LineCount);
