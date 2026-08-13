using BorealBoost.Core.Foundation;

namespace BorealBoost.App.ViewModels;

public sealed class PlaceholderViewModel : ObservableObject
{
    private string _title = "Modulo";
    private string _description = "Este modulo ainda nao esta implementado nesta fase.";

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public void Load(string routeKey)
    {
        var route = NavigationRoute.FoundationRoutes.FirstOrDefault(item => item.Key == routeKey);
        if (route is null)
        {
            Title = "Modulo indisponivel";
            Description = "A rota solicitada nao existe na Foundation.";
            return;
        }

        Title = route.DisplayName;
        Description = route.Description;
    }
}
