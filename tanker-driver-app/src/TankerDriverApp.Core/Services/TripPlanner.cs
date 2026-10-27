using TankerDriverApp.Core.Models;

namespace TankerDriverApp.Core.Services;

/// <summary>
/// Планировщик рейса водителя: распределяет заявки по секциям цистерны
/// (разные марки топлива — разные секции) и строит последовательность точек выгрузки
/// жадным алгоритмом ближайшего соседа от базы.
/// </summary>
public class TripPlanner
{
    /// <summary>Процент заполнения секции при загрузке (термическое расширение топлива).</summary>
    public const double MaxFillRatio = 0.95;

    /// <summary>Можно ли загрузить заданный объём топлива в свободные секции.</summary>
    public static bool CanLoad(Tanker tanker, FuelType fuel, double liters) =>
        UsableFreeLiters(tanker, fuel) >= liters;

    /// <summary>Суммарный полезный свободный объём для данного топлива (с учётом 95% заполнения).</summary>
    private static double UsableFreeLiters(Tanker tanker, FuelType fuel) =>
        tanker.Compartments
            .Where(c => c.Fuel is null || c.Fuel == fuel)
            .Sum(c => Math.Max(0, c.FreeLiters - c.CapacityLiters * (1 - MaxFillRatio)));

    /// <summary>Загрузка топлива по секциям. Возвращает фактический залитый объём.</summary>
    public static double LoadCompartments(Tanker tanker, FuelType fuel, double liters)
    {
        double loaded = 0;
        foreach (var comp in tanker.Compartments
                     .Where(c => c.Fuel is null || c.Fuel == fuel)
                     .OrderBy(c => c.Index))
        {
            var room = comp.FreeLiters - comp.CapacityLiters * (1 - MaxFillRatio);
            if (room <= 0) continue;
            var pour = Math.Min(room, liters - loaded);
            comp.CurrentVolumeLiters += pour;
            comp.Fuel = fuel;
            loaded += pour;
            if (loaded >= liters) break;
        }
        return Math.Round(loaded, 1);
    }

    /// <summary>Выгрузка объёма из секций с указанным топливом.</summary>
    public static double Unload(Tanker tanker, FuelType fuel, double liters)
    {
        double unloaded = 0;
        foreach (var comp in tanker.Compartments.Where(c => c.Fuel == fuel))
        {
            var take = Math.Min(comp.CurrentVolumeLiters, liters - unloaded);
            comp.CurrentVolumeLiters -= take;
            unloaded += take;
            if (comp.CurrentVolumeLiters <= 0.1)
            {
                comp.CurrentVolumeLiters = 0;
                comp.Fuel = null;
            }
            if (unloaded >= liters) break;
        }
        return Math.Round(unloaded, 1);
    }

    /// <summary>
    /// Строит маршрут рейса: старт с базы, обход точек доставки заявок
    /// методом ближайшего соседа, возврат не обязателен.
    /// </summary>
    public static List<TripLeg> BuildRoute(Station baseStation, IEnumerable<DeliveryOrder> orders, double averageSpeedKmh = 55)
    {
        var legs = new List<TripLeg>();
        var remaining = orders
            .Where(o => o.Status is OrderStatus.Accepted or OrderStatus.Loading or OrderStatus.InTransit)
            .ToList();
        var current = baseStation;

        while (remaining.Count > 0)
        {
            var nearest = remaining
                .OrderBy(o => GeoCalculator.RoadDistanceKm(current, o.Destination))
                .First();
            var dist = GeoCalculator.RoadDistanceKm(current, nearest.Destination);
            legs.Add(new TripLeg
            {
                From = current,
                To = nearest.Destination,
                DistanceKm = dist,
                EstimatedTimeMin = GeoCalculator.EstimateTimeMin(dist, averageSpeedKmh),
                Fuel = nearest.Fuel,
                VolumeLiters = nearest.RequestedVolumeLiters
            });
            current = nearest.Destination;
            remaining.Remove(nearest);
        }
        return legs;
    }

    /// <summary>Создаёт рейс из набора заявок: загрузка + маршрут.</summary>
    public Trip CreateTrip(Tanker tanker, Station baseStation, IReadOnlyList<DeliveryOrder> orders)
    {
        var trip = new Trip { Tanker = tanker };

        foreach (var order in orders)
        {
            if (!CanLoad(tanker, order.Fuel, order.RequestedVolumeLiters))
                throw new InvalidOperationException(
                    $"Недостаточно свободных секций для {order.RequestedVolumeLiters} л {order.Fuel.Display()}");
            order.LoadedVolumeLiters = LoadCompartments(tanker, order.Fuel, order.RequestedVolumeLiters);
            order.Origin = baseStation;
            order.ActualDepartureUtc = DateTime.UtcNow;
            order.Status = OrderStatus.InTransit;
            trip.Orders.Add(order);
        }

        trip.Legs = BuildRoute(baseStation, orders);
        return trip;
    }
}
