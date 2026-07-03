namespace BIKEMATES_ADMIN.Pages.Intro;

public partial class AppIntro : ContentPage
{
    public AppIntro()
    {
        InitializeComponent();
    }

    private async void OnLetGoClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AppIntro2());
    }
}