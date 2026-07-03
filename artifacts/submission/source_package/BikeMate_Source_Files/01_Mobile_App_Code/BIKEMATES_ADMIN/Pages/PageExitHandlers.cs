namespace BIKEMATES_ADMIN.Pages;

public partial class MainPage { private async void SignOut_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(SignOut)); }
public partial class MenuPage { private async void SignOut_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(SignOut)); }

internal static class ShopAdminNavigation
{
    public static async Task BackOrMenuAsync()
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.Navigation.PopAsync();
            return;
        }

        await Shell.Current.GoToAsync("//AdminTabs/Menu");
    }
}

public partial class Admins { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Calendar { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class HelpSupport { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Logistics { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Messages { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Notifications { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Operations { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Products { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Reports { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class Settings { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class ShopProfile { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
public partial class SignOut { private async void Exit_Clicked(object sender, EventArgs e) => await ShopAdminNavigation.BackOrMenuAsync(); }
