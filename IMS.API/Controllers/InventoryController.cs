using IMS.Application.DTOs;
using IMS.Application.Exceptions;
using IMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("sale")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProcessSale([FromBody] SalesOrderRequest request)
    {
        try
        {
            long orderId = await _inventoryService.ProcessSalesOrderAsync(request);
            return Ok(new { Message = "Sales order fulfilled successfully under FIFO policy.", OrderId = orderId });
        }
        catch (InsufficientStockException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (InventoryConcurrencyException ex)
        {
            // Problem 2: Return a distinct 409 Conflict status code when a collision occurs
            return Conflict(new { Error = ex.Message });
        }
    }
}