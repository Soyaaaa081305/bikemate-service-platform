namespace BIKEMATES_ADMIN.Pages.Intro;

public partial class AppIntro3 : ContentPage
{
    public AppIntro3() => InitializeComponent();

    private async void OnContinueClicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new AppIntro4());
}