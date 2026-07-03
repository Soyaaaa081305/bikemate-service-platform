using BikeMate.Api.Services;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public sealed class ReportsController(IAdminReportService reports) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        return Ok(await reports.GetRevenueAsync(from ?? DateTime.UtcNow.AddMonths(-1), to ?? DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("top-services")]
    public async Task<ActionResult<IReadOnlyCollection<TopServiceDto>>> TopServices(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetTopServicesAsync(cancellationToken));
    }

    [HttpGet("top-mechanics")]
    public async Task<ActionResult<IReadOnlyCollection<TopMechanicDto>>> TopMechanics(CancellationToken cancellationToken)
    {
        return Ok(await reports.GetTopMechanicsAsync(cancellationToken));
    }
}
