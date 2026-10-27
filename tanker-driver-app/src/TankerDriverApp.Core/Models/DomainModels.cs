namespace TankerDriverApp.Core.Models;

/// <summary>Статус заявки на перевозку.</summary>
public enum OrderStatus
{
    New,            // новая
    Accepted,       // принята водителем
    Loading,        // на загрузке
    InTransit,      // в пути
    Delivering,     // выгрузка на точке
    Completed,      // выполнена
    Cancelled,      // отменена
    ProblemReported // с проблемой (авария, недолив и т.п.)
}

/// <summary>Секция (отсек) цистерны бензовоза.</summary>
public class Compartment
{
    public int Index { get; set; }
    public double CapacityLiters { get; set; }
    public double CurrentVolumeLiters { get; set; }
    public FuelType? Fuel { get; set; }

    public double FreeLiters => CapacityLiters - CurrentVolumeLiters;
    public double FillPercent => CapacityLiters > 0
        ? Math.Round(CurrentVolumeLiters / CapacityLiters * 100, 1)
        : 0;
}

/// <summary>Бензовоз (транспортное средство).</summary>
public class Tanker
{
    public string PlateNumber { get; set; } = "";
    public string Model { get; set; } = "";
    public int CapacityLiters => (int)Compartments.Sum(c => c.CapacityLiters);
    public double SpeedKmh { get; set; } = 60;
    public List<Compartment> Compartments { get; set; } = new();
    public double OdometerKm { get; set; }

    public double CurrentVolumeLiters => Compartments.Sum(c => c.CurrentVolumeLiters);
}

/// <summary>Точка маршрута: НПЗ/база или АЗС получателя.</summary>
public class Station
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsBase { get; set; }
}

/// <summary>Позиция GPS.</summary>
public record GeoPosition(double Latitude, double Longitude, double SpeedKmh, DateTime TimestampUtc);

/// <summary>Запись telematics-журнала.</summary>
public class TrackPoint
{
    public GeoPosition Position { get; set; } = new(0, 0, 0, DateTime.UtcNow);
    public double TotalVolumeLiters { get; set; }
    public bool EngineOn { get; set; }
}

/// <summary>Этап маршрута (поездка).</summary>
public class TripLeg
{
    public Station From { get; set; } = new();
    public Station To { get; set; } = new();
    public double DistanceKm { get; set; }
    public double EstimatedTimeMin { get; set; }
    public FuelType Fuel { get; set; }
    public double VolumeLiters { get; set; }
    public bool Arrived { get; set; }
}

/// <summary>Заявка на перевозку для водителя.</summary>
public class DeliveryOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public FuelType Fuel { get; set; }
    public double RequestedVolumeLiters { get; set; }
    public double LoadedVolumeLiters { get; set; }
    public double DeliveredVolumeLiters { get; set; }
    public Station Origin { get; set; } = new();
    public Station Destination { get; set; } = new();
    public DateTime PlannedDepartureUtc { get; set; }
    public DateTime PlannedArrivalUtc { get; set; }
    public DateTime? ActualDepartureUtc { get; set; }
    public DateTime? ActualArrivalUtc { get; set; }
    public double PriceRub { get; set; }
    public string Comment { get; set; } = "";
    public List<string> PhotoProofUrls { get; set; } = new();

    /// <summary>Отклонение объёма при выгрузке, %.</summary>
    public double LossPercent => LoadedVolumeLiters > 0
        ? Math.Round((LoadedVolumeLiters - DeliveredVolumeLiters) / LoadedVolumeLiters * 100, 2)
        : 0;

    /// <summary>Допустимые транспортные потери (ГОСТ Р 8.589, ~0.5–1.5% в зависимости от вида топлива).</summary>
    public double AllowedLossPercent => Fuel switch
    {
        FuelType.Diesel or FuelType.Mazut => 1.5,
        _ => 0.9 // светлые нефтепродукты — испаряются сильнее
    };

    public bool IsLossWithinNorm => LossPercent <= AllowedLossPercent;
}

/// <summary>Рейс водителя на смену: набор заявок и поездок.</summary>
public class Trip
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedUtc { get; set; }
    public Tanker Tanker { get; set; } = new();
    public List<DeliveryOrder> Orders { get; set; } = new();
    public List<TripLeg> Legs { get; set; } = new();
    public List<TrackPoint> Track { get; set; } = new();

    public double TotalDistanceKm => Legs.Sum(l => l.DistanceKm);
    public double TotalEstimatedTimeMin => Legs.Sum(l => l.EstimatedTimeMin);
    public double TotalRevenueRub => Orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.PriceRub);
    public IEnumerable<DeliveryOrder> ActiveOrders => Orders.Where(o => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled));
}
