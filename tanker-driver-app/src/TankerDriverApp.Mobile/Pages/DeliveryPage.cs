using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile.Pages;

/// <summary>Экран выгрузки: приёмка объёма, фото пломб, подтверждение по pump-метру.</summary>
public class DeliveryPage : ContentPage
{
    public DeliveryPage(DeliveryViewModel vm, TripViewModel tripVm)
    {
        BindingContext = vm;
        Title = "Выгрузка";

        // при появлении страницы берём первую активную заявку рейса
        Appearing += (_, _) =>
        {
            if (vm.Order is null)
                vm.Order = tripVm.CurrentTrip?.ActiveOrders.FirstOrDefault();
        };

        var orderLabel = new Label { FontSize = 15, FontAttributes = FontAttributes.Bold };
        orderLabel.SetBinding(Label.TextProperty, new Binding(
            $"{nameof(DeliveryViewModel.Order)}",
            converter: new OrderHeaderConverter()));

        var acceptedEntry = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "Принято по pump-метру, л" };
        acceptedEntry.SetBinding(Entry.TextProperty, nameof(DeliveryViewModel.AcceptedLiters), BindingMode.TwoWay);

        var lossLabel = new Label { TextColor = Color.FromArgb("#DC2626") };
        lossLabel.SetBinding(Label.TextProperty, new Binding(
            nameof(DeliveryViewModel.LossPercent), stringFormat: "Отклонение: {0}%"));

        var photoButton = new Button { Text = "📸 Сфотографировать пломбу/прибор", BackgroundColor = Color.FromArgb("#2563EB"), TextColor = Colors.White };
        photoButton.SetBinding(Button.CommandProperty, nameof(DeliveryViewModel.TakePhotoCommand));

        var confirmButton = new Button { Text = "✔ Подтвердить выгрузку", BackgroundColor = Color.FromArgb("#16A34A"), TextColor = Colors.White, HeightRequest = 52 };
        confirmButton.SetBinding(Button.CommandProperty, nameof(DeliveryViewModel.ConfirmDeliveryCommand));

        var messageLabel = new Label { FontSize = 13 };
        messageLabel.SetBinding(Label.TextProperty, nameof(DeliveryViewModel.Message));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 14,
                Children = { orderLabel, acceptedEntry, lossLabel, photoButton, confirmButton, messageLabel }
            }
        };
    }

    private sealed class OrderHeaderConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is Core.Models.DeliveryOrder o
                ? $"{o.Id} · {o.Destination.Name}\n{o.Fuel.Display()}: загружено {o.LoadedVolumeLiters:F0} л"
                : "Нет активной заявки — начните смену на вкладке «Рейс».";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
