using Microsoft.EntityFrameworkCore;

public class PurchaseOrderService
{
    private readonly ErpDbContext _db;

    public PurchaseOrderService(ErpDbContext db)
    {
        _db = db;
    }

    public PurchaseOrder? GetById(int id)
    {
        return _db.PurchaseOrders
            .Include(order => order.Lines)
            .SingleOrDefault(order => order.Id == id);
    }

    // 采购订单列表
    public List<PurchaseOrderSummary> GetAll()
    {
        return _db.PurchaseOrders
        .OrderByDescending(order => order.Id)
        .Select(order => new PurchaseOrderSummary(
            order.Id,
            order.SupplierCode,
            order.Status,
            order.Lines.Count))
        .ToList();
    }

    public (PurchaseOrder? Order, string? Error) Create(
        CreatePurchaseOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierCode) ||
            _db.Suppliers.Find(request.SupplierCode) is null)
        {
            return (null, "请选择已存在的供应商。");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return (null, "采购单至少需要一条明细。");
        }

        var order = new PurchaseOrder
        {
            SupplierCode = request.SupplierCode,
            Status = "Draft"
        };

        foreach (CreatePurchaseOrderLineRequest line in request.Lines)
        {
            if (line is null ||
                string.IsNullOrWhiteSpace(line.MaterialCode) ||
                line.Quantity <= 0)
            {
                return (null, "每条明细都需要物料编码，且数量必须大于零。");
            }

            Material? material = _db.Materials.Find(line.MaterialCode);

            if (material is null)
            {
                return (null, $"物料 {line.MaterialCode} 不存在。");
            }

            if (!material.IsActive)
            {
                return (null, $"物料 {line.MaterialCode} 已停用。");
            }

            order.Lines.Add(new PurchaseOrderLine
            {
                MaterialCode = material.Code,
                Quantity = line.Quantity
            });
        }

        _db.PurchaseOrders.Add(order);
        _db.SaveChanges();

        return (order, null);
    }
}