namespace BikeMate.Core.Constants;

public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Mechanic = "Mechanic";
    public const string ShopAdmin = "ShopAdmin";
    public const string SystemAdmin = "SystemAdmin";

    public static readonly string[] All =
    [
        Customer,
        Mechanic,
        ShopAdmin,
        SystemAdmin
    ];
}
