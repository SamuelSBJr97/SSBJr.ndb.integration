using SSBJr.ndb.integration.ViewModels;

namespace SSBJr.ndb.integration.Views;

public partial class DatabaseConfigPage : ContentPage
{
    private readonly DatabaseConfigViewModel _viewModel;

    public DatabaseConfigPage(DatabaseConfigViewModel viewModel)
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