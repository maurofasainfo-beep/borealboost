using BorealBoost.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace BorealBoost.App.Pages;

public sealed partial class OptimizationPage : Page
{
    private readonly OptimizationViewModel _viewModel;
    private CancellationTokenSource? _pageCancellation;

    public OptimizationPage(OptimizationViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RunPageActionAsync(async () =>
        {
            _pageCancellation?.Dispose();
            _pageCancellation = new CancellationTokenSource();
            await _viewModel.InitializeAsync(_pageCancellation.Token);
        });
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _pageCancellation?.Cancel();
        _pageCancellation?.Dispose();
        _pageCancellation = null;
    }

    private async void OnDryRunClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RunPageActionAsync(async () =>
        {
            _pageCancellation ??= new CancellationTokenSource();
            await _viewModel.RunDryRunAsync(_pageCancellation.Token);
        });
    }

    private async void OnExecuteControlledClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await RunPageActionAsync(async () =>
        {
            _pageCancellation ??= new CancellationTokenSource();
            await _viewModel.ExecuteControlledOperationAsync(_pageCancellation.Token);
        });
    }

    private static async Task RunPageActionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }
}
