using System.Security.Claims;

namespace BikeMate.Api.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("user_id") ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
