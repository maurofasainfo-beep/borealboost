using BorealBoost.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BorealBoost.App.Pages;

public sealed partial class ScannerPage : Page
{
    private readonly ScannerViewModel _viewModel;

    public ScannerPage(ScannerViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void OnStartScanClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await _viewModel.StartScanAsync();
    }

    private void OnCancelScanClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.CancelScan();
    }
}
