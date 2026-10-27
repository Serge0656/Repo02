using TankerDriverApp.Mobile.Pages;
using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // DI-регистрация
        builder.Services.AddSingleton<TripViewModel>();
        builder.Services.AddSingleton<LoadViewModel>();
        builder.Services.AddSingleton<DeliveryViewModel>();
        builder.Services.AddSingleton<SafetyViewModel>();

        // Страницы
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<ShellMain>();
        builder.Services.AddSingleton<HomeTab>();
        builder.Services.AddSingleton<LoadPage>();
        builder.Services.AddSingleton<RoutePage>();
        builder.Services.AddSingleton<DeliveryPage>();
        builder.Services.AddSingleton<SafetyPage>();
        builder.Services.AddSingleton<ProfilePage>();

        return builder.Build();
    }
}
