namespace BikeMate
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
            Helpers.AppVisualPolish.Apply((View)Content);
        }
    }
}
