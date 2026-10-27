using System.Text;
using TankerDriverApp.Core.Models;
using TankerDriverApp.Core.Services;
using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile.Pages;

/// <summary>Экран маршрута: последовательность точек рейса с расстояниями и ETA.</summary>
public class RoutePage : ContentPage
{
    public RoutePage(TripViewModel vm)
    {
        BindingContext = vm;
        Title = "Маршрут";

        var routeInfo = new Label { FontSize = 14 };
        routeInfo.SetBinding(Label.TextProperty, new Binding(nameof(TripViewModel.CurrentTrip),
            converter: new RouteSummaryConverter()));

        Content = new ScrollView
        {
            Content = new VerticalStackLayout { Padding = 20, Spacing = 12, Children = { routeInfo } }
        };
    }

    /// <summary>Конвертер: Trip → читаемое описание маршрута (номера точек, км, минуты).</summary>
    private sealed class RouteSummaryConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Trip trip || trip.Legs.Count == 0)
                return "Маршрут появится после начала смены.";

            var sb = new StringBuilder();
            sb.AppendLine($"🚛 Рейс {trip.Id} · всего {trip.TotalDistanceKm:F1} км, ~{trip.TotalEstimatedTimeMin:F0} мин");
            for (var i = 0; i < trip.Legs.Count; i++)
            {
                var leg = trip.Legs[i];
                var mark = leg.Arrived ? "✅" : $"{i + 1}.";
                sb.AppendLine($"{mark} {leg.To.Name} — {leg.DistanceKm} км (~{leg.EstimatedTimeMin:F0} мин), " +
                              $"{leg.VolumeLiters:F0} л {leg.Fuel.Display()}");
            }
            return sb.ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
