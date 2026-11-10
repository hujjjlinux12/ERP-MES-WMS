public class SupplierService
{
    private readonly ErpDbContext _db;

    public SupplierService(ErpDbContext db)
    {
        _db = db;
    }
    // 查询所有供应商
    public List<Supplier> GetSuppliers()
    {
        return _db.Suppliers.OrderBy(supplier => supplier.Code).ToList();
    }

    // 添加供应商
    public bool AddSupplier(Supplier supplier)
    {
        if(_db.Suppliers.Find(supplier.Code) is not null)
        {
            return false;
        }
        _db.Suppliers.Add(supplier);
        _db.SaveChanges();
        return true;
    }

    // 修改供应商
    public bool UpdateSupplier(string code,string name)
    {
     Supplier? supplier = _db.Suppliers.Find(code);
     if (supplier is null)
        {
            return false;   
        } 
        supplier.Name = name;
        _db.SaveChanges();
        return true;
    }

    // 删除供应商
    public bool DeleteSupplier(string code)
    {
        Supplier? supplier = _db.Suppliers.Find(code);
        if (supplier is null)
        {
            return false;   
        } 
        _db.Suppliers.Remove(supplier);
        _db.SaveChanges();
        return true;
    }
}