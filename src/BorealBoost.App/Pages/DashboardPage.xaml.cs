using BorealBoost.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BorealBoost.App.Pages;

public sealed partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.ProbeAgentAsync(CancellationToken.None);
    }
}
