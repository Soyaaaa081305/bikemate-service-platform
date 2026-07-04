using Microsoft.Extensions.DependencyInjection;

namespace BikeMate
{
    public partial class App : Application
    {
        public App()
        {
            Services.CrashLogService.Install("BikeMate");
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
