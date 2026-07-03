namespace BIKEMATES_ADMIN.Pages.Intro;

public partial class AppIntro2 : ContentPage
{
    public AppIntro2() => InitializeComponent();

    private async void OnContinueClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(new AppIntro3());
}