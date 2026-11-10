using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/suppliers")]
public class SupplierController:ControllerBase
{
    private readonly SupplierService _supplierService;
    public SupplierController(SupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    // 查询供应商列表
    [HttpGet]
    public List<Supplier> GetSuppliers()
    {
        return _supplierService.GetSuppliers();
    }

    // 添加供应商
    [HttpPost]
    public ActionResult CreateSupplier([FromBody] Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Code) || 
            string.IsNullOrWhiteSpace(supplier.Name))
        {
            return BadRequest("供应商编码和名称不能为空");
        }
        bool added = _supplierService.AddSupplier(supplier);
        if(!added)
        {
            return Conflict("供应商编码已存在");
        }
        return StatusCode(201,supplier); // 返回201 Created状态码
    }
}