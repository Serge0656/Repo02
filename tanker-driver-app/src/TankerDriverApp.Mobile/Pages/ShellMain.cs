namespace TankerDriverApp.Mobile.Pages;

/// <summary>Навигация приложения: вкладки Рейс / Загрузка / Маршрут / Выгрузка / Безопасность / Профиль.</summary>
public class ShellMain : Shell
{
    public ShellMain(HomeTab home, LoadPage load, RoutePage route, DeliveryPage delivery, SafetyPage safety, ProfilePage profile)
    {
        var homeShell = new ShellContent { Title = "Рейс", Content = home, Icon = "home.png" };
        var loadShell = new ShellContent { Title = "Загрузка", Content = load, Icon = "drop.png" };
        var routeShell = new ShellContent { Title = "Маршрут", Content = route, Icon = "map.png" };
        var deliveryShell = new ShellContent { Title = "Выгрузка", Content = delivery, Icon = "truck.png" };
        var safetyShell = new ShellContent { Title = "SOS/Безопасность", Content = safety, Icon = "shield.png" };
        var profileShell = new ShellContent { Title = "Профиль", Content = profile, Icon = "user.png" };

        Items.Add(new ShellItem
        {
            Title = "Смена",
            Items = { homeShell, loadShell, routeShell, deliveryShell }
        });
        Items.Add(new ShellItem
        {
            Title = "Ещё",
            Items = { safetyShell, profileShell }
        });
    }
}
