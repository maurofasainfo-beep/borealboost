using BorealBoost.App.Navigation;
using BorealBoost.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace BorealBoost.App;

public sealed partial class MainWindow : Window
{
    private const int MinWindowWidth = 1000;
    private const int MinWindowHeight = 700;

    private readonly INavigationService _navigationService;

    public MainWindow(MainViewModel viewModel, INavigationService navigationService)
    {
        ViewModel = viewModel;
        _navigationService = navigationService;

        InitializeComponent();
        _navigationService.Initialize(ContentFrame);
        AppWindow.Resize(new SizeInt32(1180, 760));
        AppWindow.Changed += OnAppWindowChanged;

        RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
        _navigationService.Navigate("Dashboard");
    }

    public MainViewModel ViewModel { get; }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string routeKey })
        {
            _navigationService.Navigate(routeKey);
        }
    }

    private void OnAppWindowChanged(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
    {
        if (!args.DidSizeChange)
        {
            return;
        }

        var width = Math.Max(sender.Size.Width, MinWindowWidth);
        var height = Math.Max(sender.Size.Height, MinWindowHeight);
        if (width != sender.Size.Width || height != sender.Size.Height)
        {
            sender.Resize(new SizeInt32(width, height));
        }
    }
}
