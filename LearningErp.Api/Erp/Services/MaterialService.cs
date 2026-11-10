public class MaterialService
{
    private readonly ErpDbContext _db;
    // 模拟数据库存储物料信息
    public MaterialService(ErpDbContext db)
    {
        _db = db;
    }
    public List<Material> GetMaterials(bool? isActive = null)
    {
        IQueryable<Material> query = _db.Materials;
        if(isActive is bool active)
        {
            query = query.Where(material => material.IsActive == active);
        }
        return query.ToList();
    }
   
    // 通过code查询物料
    public Material? GetMaterialByCode(string code)
    {
        return _db.Materials.Find(code);
    }

    // 添加物料
    public bool AddMaterial(Material material)
    {
        if(GetMaterialByCode(material.Code) is not null)
        {
            return false;
        }
        _db.Materials.Add(material);
        _db.SaveChanges();
        return true;
    }

    // 修改物料
    public bool UpdateMaterial(string code,string name,string unit)
    {
     Material? material = GetMaterialByCode(code);
     if (material is null)
        {
            return false;   
        } 
        material.Name = name;
        material.Unit = unit;
        _db.SaveChanges();
        return true;
    }

    // 删除物料
    public bool DeleteMaterial(string code)
    {
        Material? material = GetMaterialByCode(code);
        if (material is null)
        {
            return false;   
        } 
        _db.Materials.Remove(material);
        _db.SaveChanges();
        return true;
    }

    // 设置物料状态
    public bool SetMaterialStatus(string code,bool isActive)
    {
        Material? material = GetMaterialByCode(code);
        if (material is null)
        {
            return false;
        }
        material.IsActive = isActive;
        _db.SaveChanges();
        return true;
    }
}