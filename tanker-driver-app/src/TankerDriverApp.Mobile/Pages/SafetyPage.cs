using TankerDriverApp.Core.Services;
using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile.Pages;

/// <summary>Экран безопасности: тахограф, скорость, журнал оповещений, SOS.</summary>
public class SafetyPage : ContentPage
{
    public SafetyPage(SafetyViewModel vm)
    {
        BindingContext = vm;
        Title = "Безопасность";

        var drivingLabel = new Label { FontSize = 15 };
        drivingLabel.SetBinding(Label.TextProperty, new Binding(
            nameof(SafetyViewModel.DrivingHoursElapsed), stringFormat: "⏱ Непрерывно за рулём: {0} ч"));

        var breakLabel = new Label { TextColor = Color.FromArgb("#B45309") };
        breakLabel.SetBinding(Label.TextProperty, new Binding(
            nameof(SafetyViewModel.BreakInHoursLeft), stringFormat: "До обязательного перерыва: {0} ч"));

        var speedEntry = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "Текущая скорость, км/ч" };
        speedEntry.SetBinding(Entry.TextProperty, nameof(SafetyViewModel.CurrentSpeedKmh), BindingMode.TwoWay);

        var checkButton = new Button { Text = "🔍 Проверить сейчас", BackgroundColor = Color.FromArgb("#2563EB"), TextColor = Colors.White };
        checkButton.SetBinding(Button.CommandProperty, nameof(SafetyViewModel.CheckNowCommand));

        var breakButton = new Button { Text = "☕ Я на перерыве", BackgroundColor = Color.FromArgb("#16A34A"), TextColor = Colors.White };
        breakButton.SetBinding(Button.CommandProperty, nameof(SafetyViewModel.StartBreakCommand));

        var alertsList = new CollectionView { HeightRequest = 220 };
        alertsList.SetBinding(ItemsView.ItemsSourceProperty, nameof(SafetyViewModel.Alerts));
        alertsList.ItemTemplate = new DataTemplate(() =>
        {
            var lbl = new Label { FontSize = 13, Margin = new Thickness(0, 4) };
            lbl.SetBinding(Label.TextProperty, new Binding(
                ".", converter: new AlertConverter()));
            return lbl;
        });

        var sosButton = new Button
        {
            Text = "🆘 SOS",
            BackgroundColor = Color.FromArgb("#DC2626"),
            TextColor = Colors.White,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 72
        };
        sosButton.SetBinding(Button.CommandProperty, nameof(SafetyViewModel.SendSosCommand));

        var sosLabel = new Label { HorizontalTextAlignment = TextAlignment.Center };
        sosLabel.SetBinding(Label.TextProperty, nameof(SafetyViewModel.SosMessage));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children = { drivingLabel, breakLabel, speedEntry, checkButton, breakButton, alertsList, sosButton, sosLabel }
            }
        };
    }

    private sealed class AlertConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is SafetyMonitor.SafetyAlert a
                ? $"{a.Level switch { SafetyMonitor.Severity.Critical => "🔴", SafetyMonitor.Severity.Warning => "🟠", _ => "🔵" }} {a.Message}"
                : "";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
