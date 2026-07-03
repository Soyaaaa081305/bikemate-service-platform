namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreateShopNotFound : ContentPage
{
    public AccCreateShopNotFound()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
