using SSBJr.ndb.integration.ViewModels;

namespace SSBJr.ndb.integration.Views;

public partial class MonitoringPage : ContentPage
{
    private readonly MonitoringViewModel _viewModel;

    public MonitoringPage(MonitoringViewModel viewModel)
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
}