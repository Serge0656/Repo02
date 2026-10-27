using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile.Pages;

/// <summary>Экран входа: табельный номер + PIN (в демо — любой).</summary>
public class LoginPage : ContentPage
{
    public LoginPage(IServiceProvider services)
    {
        BackgroundColor = Color.FromArgb("#0F172A");

        var title = new Label
        {
            Text = "🚛 Бензовоз · Водитель",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 40),
            TextColor = Colors.White
        };

        var pinEntry = new Entry
        {
            Placeholder = "PIN",
            IsPassword = true,
            BackgroundColor = Color.FromArgb("#1E293B"),
            TextColor = Colors.White,
            Keyboard = Keyboard.Numeric
        };

        var loginButton = new Button
        {
            Text = "Войти в смену",
            BackgroundColor = Color.FromArgb("#F59E0B"),
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 56
        };
        loginButton.Clicked += (_, _) =>
        {
            if (Application.Current is App app)
                app.OpenMainShell();
        };

        var hint = new Label
        {
            Text = "Демо-режим: PIN не проверяется",
            TextColor = Color.FromArgb("#64748B"),
            HorizontalOptions = LayoutOptions.Center,
            FontSize = 12
        };

        Content = new VerticalStackLayout
        {
            Padding = 32,
            Spacing = 14,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                title,
                pinEntry,
                loginButton,
                hint
            }
        };
    }
}
