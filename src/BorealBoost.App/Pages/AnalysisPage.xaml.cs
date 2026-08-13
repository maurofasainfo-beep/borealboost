using BorealBoost.App.ViewModels;
using BorealBoost.Core.Analysis;
using Microsoft.UI.Xaml.Controls;

namespace BorealBoost.App.Pages;

public sealed partial class AnalysisPage : Page
{
    private readonly AnalysisViewModel _viewModel;
    private CancellationTokenSource? _pageCancellation;

    public AnalysisPage(AnalysisViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _pageCancellation?.Dispose();
        _pageCancellation = new CancellationTokenSource();
        await RunSafelyAsync(
            token => _viewModel.InitializeAsync(token),
            "analysis.page.loaded_failed");
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.CancelActiveAnalysis();
        _pageCancellation?.Cancel();
        _pageCancellation?.Dispose();
        _pageCancellation = null;
    }

    private async void OnAnalyzeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_pageCancellation is null)
        {
            _pageCancellation = new CancellationTokenSource();
        }

        await RunSafelyAsync(
            token => _viewModel.AnalyzeCurrentSnapshotAsync(token),
            "analysis.page.manual_analyze_failed");
    }

    private void OnAllPresetClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SetPresetFilter(null);
    }

    private void OnBasicPresetClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SetPresetFilter(RecommendationPreset.Basic);
    }

    private void OnMediumPresetClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SetPresetFilter(RecommendationPreset.Medium);
    }

    private void OnAdvancedPresetClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SetPresetFilter(RecommendationPreset.Advanced);
    }

    private void OnCategoryFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryFilter.SelectedItem is ComboBoxItem { Tag: string tag })
        {
            _viewModel.SetCategoryFilter(tag == "All" ? null : tag);
        }
    }

    private void OnRiskFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RiskFilter.SelectedItem is ComboBoxItem { Tag: string tag })
        {
            _viewModel.SetRiskFilter(tag == "All" ? null : tag);
        }
    }

    private async Task RunSafelyAsync(Func<CancellationToken, Task> action, string context)
    {
        var token = _pageCancellation?.Token ?? CancellationToken.None;
        try
        {
            await action(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _viewModel.ReportUnexpectedException(exception, context);
        }
    }
}
