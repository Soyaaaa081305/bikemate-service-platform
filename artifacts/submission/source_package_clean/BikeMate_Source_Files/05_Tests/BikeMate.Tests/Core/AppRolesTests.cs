using BikeMate.Core.Constants;

namespace BikeMate.Tests.Core;

public sealed class AppRolesTests
{
    [Fact]
    public void All_ContainsAllDefinedRoles()
    {
        Assert.Contains(AppRoles.Customer, AppRoles.All);
        Assert.Contains(AppRoles.Mechanic, AppRoles.All);
        Assert.Contains(AppRoles.ShopAdmin, AppRoles.All);
        Assert.Contains(AppRoles.SystemAdmin, AppRoles.All);
    }

    [Fact]
    public void All_HasExactlyFourRoles()
    {
        Assert.Equal(4, AppRoles.All.Length);
    }

    [Fact]
    public void All_ContainsNoDuplicates()
    {
        Assert.Equal(AppRoles.All.Length, AppRoles.All.Distinct().Count());
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Mechanic")]
    [InlineData("ShopAdmin")]
    [InlineData("SystemAdmin")]
    public void RoleConstants_HaveExpectedValues(string expectedRole)
    {
        Assert.Contains(expectedRole, AppRoles.All);
    }
}
