using SSBJr.ndb.integration.ViewModels;

namespace SSBJr.ndb.integration.Views;

public partial class InfrastructurePage : ContentPage
{
    private readonly InfrastructureViewModel _viewModel;

    public InfrastructurePage(InfrastructureViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SettingsPage");
    }
}