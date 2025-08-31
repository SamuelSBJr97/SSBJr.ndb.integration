using SSBJr.ndb.integration.ViewModels;

namespace SSBJr.ndb.integration.Views;

public partial class ApiDetailPage : ContentPage
{
    private readonly ApiDetailViewModel _viewModel;

    public ApiDetailPage(ApiDetailViewModel viewModel)
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