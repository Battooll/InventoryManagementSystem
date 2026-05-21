using IMS.Application.DTOs;
using IMS.Application.Exceptions;
using IMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private readonly IStockTransferService _transferService;

    public TransferController(IStockTransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateTransfer([FromBody] InitiateTransferRequest request)
    {
        try
        {
            long transferId = await _transferService.InitiateTransferAsync(request);
            return Ok(new { Message = "Stock transfer initialized. Inventory marked In-Transit.", TransferId = transferId });
        }
        catch (InsufficientStockException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteTransfer([FromBody] CompleteTransferRequest request)
    {
        try
        {
            await _transferService.CompleteTransferAsync(request);
            return Ok(new { Message = "Stock transfer confirmed. Stock successfully added to destination warehouse." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }
}