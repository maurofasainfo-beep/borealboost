using BorealBoost.App.Navigation;
using BorealBoost.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BorealBoost.App.Pages;

public sealed partial class DashboardPage : Page
{
    private readonly INavigationService _navigationService;
    private readonly DashboardViewModel _viewModel;
    private CancellationTokenSource? _loadedCancellation;

    public DashboardPage(DashboardViewModel viewModel, INavigationService navigationService)
    {
        _navigationService = navigationService;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _loadedCancellation?.Dispose();
        _loadedCancellation = new CancellationTokenSource();
        await _viewModel.ProbeAgentAsync(_loadedCancellation.Token);
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _loadedCancellation?.Cancel();
        _loadedCancellation?.Dispose();
        _loadedCancellation = null;
    }

    private void OnAnalyzeComputerClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _navigationService.Navigate("Scanner");
    }
}
