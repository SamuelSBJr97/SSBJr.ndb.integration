namespace SSBJr.ndb.integration
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnManageApisClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ApiManagerPage");
        }

        private async void OnInfrastructureClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//InfrastructurePage");
        }

        private async void OnMonitoringClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//MonitoringPage");
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//SettingsPage");
        }

        private async void OnGetStartedClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ApiManagerPage");
        }
    }
}
