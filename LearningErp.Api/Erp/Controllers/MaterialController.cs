using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/materials")]
public class MaterialController:ControllerBase
{
    private readonly MaterialService _materialService;

    public MaterialController(MaterialService materialService)
    {
     _materialService = materialService;   
    }

    // 查询物料列表
    [HttpGet]
    public List<Material> GetMaterials([FromQuery] bool? isActive)
    {
        return _materialService.GetMaterials(isActive);
    }

    // 通过code查询物料
    [HttpGet("{code}")]
    public ActionResult<Material> GetMaterialByCode(string code)
    {
        Material? material = _materialService.GetMaterialByCode(code);
        if(material is null)
        {
            return NotFound();
        }

        return Ok(material) ; 
    }

    // 添加物料
    [HttpPost]
    public ActionResult CreateMaterial([FromBody]Material material)
    {
        if (string.IsNullOrWhiteSpace(material.Code) || 
            string.IsNullOrWhiteSpace(material.Name) || 
            string.IsNullOrWhiteSpace(material.Unit))
        {
            return BadRequest("物料编码、名称和单位不能为空");
        }
        bool added = _materialService.AddMaterial(material);
        if(!added)
        {
            return Conflict("物料编码已存在");
        }
        return CreatedAtAction(nameof(GetMaterialByCode), new { code = material.Code }, material);
    
    }

    // 修改物料
    [HttpPut("{code}")]
    public ActionResult UpdateMaterial([FromRoute] string code,[FromBody] UpdateMaterialRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Unit))
        {
            return BadRequest("物料名称和单位不能为空");
        }
        bool updated = _materialService.UpdateMaterial(code,request.Name,request.Unit);
        if(!updated)
        {
            return NotFound("物料不存在");
        }
        return NoContent();
    }

    // 删除物料
    [HttpDelete("{code}")]
    public ActionResult DeleteMaterial([FromRoute] string code)
    {
        bool deleted = _materialService.DeleteMaterial(code);
        if(!deleted)
        {
            return NotFound("物料不存在");
        }
        return NoContent();
    }

    // 设置物料状态
    [HttpPut("{code}/status")]
    public ActionResult UpdateMaterialStatus([FromRoute] string code,[FromBody] UpdateMaterialRequest request)
    {
        if(request.IsActive is null)
        {
            return BadRequest("物料状态不能为空");
        }
        bool updated = _materialService.SetMaterialStatus(code,request.IsActive.Value);
        if(!updated)
        {
            return NotFound("物料不存在");
        }
        return NoContent();
    }
}