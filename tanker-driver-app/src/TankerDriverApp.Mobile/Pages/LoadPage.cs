using TankerDriverApp.Core.Models;
using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile.Pages;

/// <summary>Экран загрузки на эстакаде: выбор топлива, объёма, контроль массы.</summary>
public class LoadPage : ContentPage
{
    public LoadPage(LoadViewModel vm)
    {
        BindingContext = vm;
        Title = "Загрузка";

        var fuelPicker = new Picker { Title = "Марка топлива" };
        fuelPicker.SetBinding(Picker.ItemsSourceProperty, nameof(LoadViewModel.Fuels));
        fuelPicker.SetBinding(Picker.SelectedItemProperty, nameof(LoadViewModel.SelectedFuel), BindingMode.TwoWay);

        var litersEntry = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "Объём, л" };
        litersEntry.SetBinding(Entry.TextProperty, nameof(LoadViewModel.PlannedLiters), BindingMode.TwoWay);

        var massLabel = new Label { TextColor = Color.FromArgb("#64748B") };
        massLabel.SetBinding(Label.TextProperty, new Binding(nameof(LoadViewModel.EstimatedMassTons), stringFormat: "Расчётная масса груза: {0} т"));

        var loadButton = new Button { Text = "💧 Начать налив", BackgroundColor = Color.FromArgb("#16A34A"), TextColor = Colors.White };
        loadButton.SetBinding(Button.CommandProperty, nameof(LoadViewModel.StartLoadingCommand));
        loadButton.SetBinding(IsVisibleProperty, nameof(LoadViewModel.CanStartLoad));

        var factLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 16 };
        factLabel.SetBinding(Label.TextProperty, new Binding(nameof(LoadViewModel.FactLiters), stringFormat: "Фактически залито: {0:F0} л"));

        var messageLabel = new Label { TextColor = Color.FromArgb("#B45309"), FontSize = 13 };
        messageLabel.SetBinding(Label.TextProperty, nameof(LoadViewModel.Message));

        var sosButton = new Button { Text = "🚨 Аварийный стоп / слив", BackgroundColor = Color.FromArgb("#DC2626"), TextColor = Colors.White };
        sosButton.SetBinding(Button.CommandProperty, nameof(LoadViewModel.EmergencyStopCommand));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 14,
                Children = { fuelPicker, litersEntry, massLabel, loadButton, factLabel, messageLabel, sosButton }
            }
        };
    }
}
