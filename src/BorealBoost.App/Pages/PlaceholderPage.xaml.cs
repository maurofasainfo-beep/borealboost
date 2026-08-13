using BorealBoost.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BorealBoost.App.Pages;

public sealed partial class PlaceholderPage : Page
{
    private readonly PlaceholderViewModel _viewModel;

    public PlaceholderPage(PlaceholderViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }

    public void Load(string routeKey)
    {
        _viewModel.Load(routeKey);
    }
}
