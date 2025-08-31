using SSBJr.ndb.integration.ViewModels;

namespace SSBJr.ndb.integration.Views;

public partial class CacheConfigPage : ContentPage
{
    private readonly CacheConfigViewModel _viewModel;

    public CacheConfigPage(CacheConfigViewModel viewModel)
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

    private void OnAdvancedToggleClicked(object sender, EventArgs e)
    {
        // Toggle advanced settings visibility
        _viewModel.IsAdvancedVisible = !_viewModel.IsAdvancedVisible;
    }
}