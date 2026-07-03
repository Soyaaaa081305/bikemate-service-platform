namespace BIKEMATES_ADMIN.Pages.Intro;

public partial class AppIntro4 : ContentPage
{
    public AppIntro4() => InitializeComponent();

    private async void OnContinueClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new AppIntro5());
}