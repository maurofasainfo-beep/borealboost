using BorealBoost.App.Pages;
using Microsoft.UI.Xaml.Controls;

namespace BorealBoost.App.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly Func<DashboardPage> _dashboardPageFactory;
    private readonly Func<PlaceholderPage> _placeholderPageFactory;
    private Frame? _frame;

    public NavigationService(Func<DashboardPage> dashboardPageFactory, Func<PlaceholderPage> placeholderPageFactory)
    {
        _dashboardPageFactory = dashboardPageFactory;
        _placeholderPageFactory = placeholderPageFactory;
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public bool Navigate(string routeKey)
    {
        if (_frame is null)
        {
            return false;
        }

        if (routeKey == "Dashboard")
        {
            var page = _dashboardPageFactory();
            _frame.Content = page;
            return true;
        }

        var placeholder = _placeholderPageFactory();
        placeholder.Load(routeKey);
        _frame.Content = placeholder;
        return true;
    }
}
