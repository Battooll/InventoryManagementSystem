using IMS.Application.DTOs;
using IMS.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary([FromQuery] ReportFilterDto filter)
    {
        var reportData = await _reportService.GetInventoryReportAsync(filter);
        return Ok(reportData);
    }
}