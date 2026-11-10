using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrderController : ControllerBase
{
    private readonly PurchaseOrderService _service;

    public PurchaseOrderController(PurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}")]
    public ActionResult<PurchaseOrder> GetById([FromRoute] int id)
    {
        PurchaseOrder? order = _service.GetById(id);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [HttpPost]
    public ActionResult<PurchaseOrder> Create(
        [FromBody] CreatePurchaseOrderRequest request)
    {
        var (order, error) = _service.Create(request);

        if (order is null)
        {
            return BadRequest(error);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            order);
    }
}