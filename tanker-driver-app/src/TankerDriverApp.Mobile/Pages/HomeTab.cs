using TankerDriverApp.Core.Models;
using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile.Pages;

/// <summary>Главная вкладка: карточка рейса, список заявок, кнопки действий.</summary>
public class HomeTab : ContentPage
{
    private readonly TripViewModel _vm;
    private readonly CollectionView _ordersList;

    public HomeTab(TripViewModel vm)
    {
        _vm = vm;
        BindingContext = vm;
        Title = "Рейс";

        var startButton = new Button
        {
            Text = "▶ Начать смену (получить заявки)",
            BackgroundColor = Color.FromArgb("#16A34A"),
            TextColor = Colors.White,
            HeightRequest = 52
        };
        startButton.SetBinding(Button.CommandProperty, nameof(TripViewModel.StartShiftCommand));

        var finishButton = new Button
        {
            Text = "⏹ Завершить смену",
            BackgroundColor = Color.FromArgb("#DC2626"),
            TextColor = Colors.White
        };
        finishButton.SetBinding(Button.CommandProperty, nameof(TripViewModel.FinishShiftCommand));

        var summary = new VerticalStackLayout
        {
            Spacing = 6,
            Padding = 16,
            BackgroundColor = Color.FromArgb("#EFF6FF")
        };
        summary.Children.Add(BindLabel(nameof(TripViewModel.StatusMessage), fontSize: 15, bold: true));
        summary.Children.Add(BindLabel(nameof(TripViewModel.TotalDistanceKm), format: "Маршрут: {0:F1} км"));
        summary.Children.Add(BindLabel(nameof(TripViewModel.ActiveOrdersCount), format: "Активных заявок: {0}"));

        _ordersList = new CollectionView();
        _ordersList.ItemTemplate = new DataTemplate(() => BuildOrderCard());

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TripViewModel.CurrentTrip))
                RefreshOrders();
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout { Padding = 16, Spacing = 12, Children = { summary, startButton, _ordersList, finishButton } }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshOrders();
    }

    private void RefreshOrders() => _ordersList.ItemsSource = _vm.CurrentTrip?.Orders;

    private View BuildOrderCard()
    {
        var card = new VerticalStackLayout
        {
            Spacing = 6,
            Padding = 14,
            Margin = new Thickness(0, 4),
            BackgroundColor = Colors.White
        };

        var head = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15 };
        head.SetBinding(Label.TextProperty, $"{nameof(DeliveryOrder.Destination)}.{nameof(Station.Name)}");

        var details = new Label { TextColor = Color.FromArgb("#B45309"), FontSize = 13 };
        details.SetBinding(Label.TextProperty, new Binding(
            nameof(DeliveryOrder.RequestedVolumeLiters), stringFormat: "{0:F0} л · " + nameof(DeliveryOrder.Fuel)));

        var status = new Label { TextColor = Color.FromArgb("#64748B"), FontSize = 12 };
        status.SetBinding(Label.TextProperty, nameof(DeliveryOrder.Status));

        var arrivedBtn = new Button
        {
            Text = "📍 Я прибыл на точку",
            BackgroundColor = Color.FromArgb("#2563EB"),
            TextColor = Colors.White,
            HeightRequest = 44
        };
        arrivedBtn.SetBinding(Button.CommandProperty, new Binding(
            $"BindingContext.{nameof(TripViewModel.MarkArrivedCommand)}", source: this));
        arrivedBtn.SetBinding(Button.CommandParameterProperty, ".");

        card.Children.Add(head);
        card.Children.Add(details);
        card.Children.Add(status);
        card.Children.Add(arrivedBtn);
        return card;
    }

    private static Label BindLabel(string path, string? format = null, double fontSize = 13, bool bold = false)
    {
        var lbl = new Label { FontSize = fontSize, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None };
        lbl.SetBinding(Label.TextProperty, format is null
            ? new Binding(path)
            : new Binding(path, stringFormat: format));
        return lbl;
    }
}
