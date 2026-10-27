namespace TankerDriverApp.Mobile;

using TankerDriverApp.Mobile.Pages;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        MainPage = new LoginPage(services); // до авторизации показываем вход
    }

    /// <summary>После успешного входа открываем основной Shell приложения.</summary>
    public void OpenMainShell() => MainPage = _services.GetRequiredService<ShellMain>();
}
