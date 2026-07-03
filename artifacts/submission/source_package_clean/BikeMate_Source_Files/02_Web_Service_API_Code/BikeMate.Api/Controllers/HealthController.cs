using Microsoft.AspNetCore.Mvc;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "BikeMate.Api",
            utcNow = DateTimeOffset.UtcNow
        });
    }
}
