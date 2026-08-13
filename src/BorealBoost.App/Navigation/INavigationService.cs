using Microsoft.UI.Xaml.Controls;

namespace BorealBoost.App.Navigation;

public interface INavigationService
{
    void Initialize(Frame frame);

    bool Navigate(string routeKey);
}
