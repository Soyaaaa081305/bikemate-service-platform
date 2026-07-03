using System.Security.Claims;
using BikeMate.Api.Helpers;

namespace BikeMate.Tests.Helpers;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ReturnsUserIdFromUserIdClaim()
    {
        var claims = new[] { new Claim("user_id", "42") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        Assert.Equal(42, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_FallsBackToNameIdentifier()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "7") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        Assert.Equal(7, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_FallsBackToSubClaim()
    {
        var claims = new[] { new Claim("sub", "99") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        Assert.Equal(99, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_PrefersUserIdOverNameIdentifier()
    {
        var claims = new[]
        {
            new Claim("user_id", "10"),
            new Claim(ClaimTypes.NameIdentifier, "20"),
            new Claim("sub", "30")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        Assert.Equal(10, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ReturnsZeroWhenNoClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Equal(0, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ReturnsZeroForNonNumericClaim()
    {
        var claims = new[] { new Claim("user_id", "not-a-number") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        Assert.Equal(0, principal.GetUserId());
    }
}
