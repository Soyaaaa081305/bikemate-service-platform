using BikeMate.Core.Constants;
using BikeMate.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public sealed class UsersController(BikeMateDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Select(x => new
            {
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Email,
                x.AccountStatus,
                Roles = x.UserRoles.Select(r => r.Role!.RoleName)
            })
            .ToArrayAsync(cancellationToken));
    }
}
