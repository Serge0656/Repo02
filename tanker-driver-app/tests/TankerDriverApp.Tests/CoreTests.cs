using TankerDriverApp.Core.Models;
using TankerDriverApp.Core.Services;
using Xunit;

namespace TankerDriverApp.Tests;

public class GeoCalculatorTests
{
    [Fact]
    public void Haversine_Kreml_StPetersburg_Approx700Km()
    {
        var km = GeoCalculator.HaversineKm(55.7558, 37.6173, 59.9311, 30.3609);
        Assert.InRange(km, 630, 720); // ~634 км по прямой
    }

    [Fact]
    public void RoadDistance_IsLongerThanStraightLine()
    {
        var a = new Station { Latitude = 55.75, Longitude = 37.61 };
        var b = new Station { Latitude = 55.43, Longitude = 37.74 };
        Assert.True(GeoCalculator.RoadDistanceKm(a, b) > GeoCalculator.HaversineKm(a.Latitude, a.Longitude, b.Latitude, b.Longitude));
    }

    [Fact]
    public void EstimateTime_60Km_At55Kmh_IsAbout65Min()
    {
        Assert.Equal(65, GeoCalculator.EstimateTimeMin(60, 55), 0);
    }
}

public class TripPlannerTests
{
    private static Tanker NewTanker() => new()
    {
        PlateNumber = "X001XX77",
        Compartments = Enumerable.Range(1, 4)
            .Select(i => new Compartment { Index = i, CapacityLiters = 3000 })
            .ToList()
    };

    [Fact]
    public void LoadCompartments_RespectsMaxFillRatio()
    {
        var tanker = NewTanker();
        var loaded = TripPlanner.LoadCompartments(tanker, FuelType.Ai92, 3000);
        // 95% от 3000 = 2850 в первой секции, остальное во второй
        Assert.Equal(3000, loaded, 1);
        Assert.Equal(2850, tanker.Compartments[0].CurrentVolumeLiters, 1);
        Assert.Equal(FuelType.Ai92, tanker.Compartments[0].Fuel);
    }

    [Fact]
    public void CannotMixFuels_InSameCompartment()
    {
        var tanker = NewTanker();
        TripPlanner.LoadCompartments(tanker, FuelType.Ai95, 2850); // заполняет секцию 1 до упора
        Assert.True(TripPlanner.CanLoad(tanker, FuelType.Diesel, 1000));
        var loaded = TripPlanner.LoadCompartments(tanker, FuelType.Diesel, 1000);
        Assert.Equal(1000, loaded, 1);
        Assert.Equal(FuelType.Ai95, tanker.Compartments[0].Fuel);
        Assert.Equal(FuelType.Diesel, tanker.Compartments[1].Fuel);
    }

    [Fact]
    public void Unload_ClearsFuelWhenEmpty()
    {
        var tanker = NewTanker();
        TripPlanner.LoadCompartments(tanker, FuelType.Diesel, 2000);
        var out1 = TripPlanner.Unload(tanker, FuelType.Diesel, 2000);
        Assert.Equal(2000, out1, 1);
        Assert.Null(tanker.Compartments[0].Fuel);
        Assert.Equal(0, tanker.Compartments[0].CurrentVolumeLiters);
    }

    [Fact]
    public void CreateTrip_SetsStatusAndBuildsRoute()
    {
        var planner = new TripPlanner();
        var orders = DemoData.CreateOrders();
        var trip = planner.CreateTrip(NewTanker(), DemoData.Base(), orders);

        Assert.Equal(orders.Count, trip.Legs.Count);
        Assert.All(trip.Orders, o => Assert.Equal(OrderStatus.InTransit, o.Status));
        Assert.True(trip.TotalDistanceKm > 0);
        // первый leg всегда от базы
        Assert.True(trip.Legs[0].From.IsBase);
    }

    [Fact]
    public void CreateTrip_ThrowsWhenOverCapacity()
    {
        var planner = new TripPlanner();
        var order = new DeliveryOrder
        {
            Fuel = FuelType.Ai92,
            RequestedVolumeLiters = 20000, // больше всей цистерны
            Destination = new Station { Name = "Далеко", Latitude = 56, Longitude = 38 }
        };
        Assert.Throws<InvalidOperationException>(() =>
            planner.CreateTrip(NewTanker(), DemoData.Base(), new[] { order }));
    }
}

public class SafetyMonitorTests
{
    [Fact]
    public void Speeding_ProducesCriticalAlert()
    {
        var mon = new SafetyMonitor();
        var point = new TrackPoint
        {
            Position = new GeoPosition(55.7, 37.6, 95, DateTime.UtcNow),
            EngineOn = true
        };
        var alert = mon.CheckTrackPoint(point, null);
        Assert.NotNull(alert);
        Assert.Equal(SafetyMonitor.Severity.Critical, alert!.Level);
    }

    [Fact]
    public void DrivingSchedule_After4_5Hours_RequiresBreak()
    {
        var mon = new SafetyMonitor();
        var start = DateTime.UtcNow.AddHours(-5);
        var alert = mon.CheckDrivingSchedule(start, DateTime.UtcNow);
        Assert.Equal(SafetyMonitor.Severity.Critical, alert?.Level);
    }

    [Fact]
    public void MassCheck_DetectsOverload()
    {
        var mon = new SafetyMonitor();
        // 20 000 л бензина ≈ 14.6 т + тара 9.5 = 24.1 — ок; дизель 20 000 л = 16.8 + 9.5 = 26.3 — перегруз
        Assert.Null(mon.CheckMass(new Tanker(), FuelType.Ai92, 20000));
        var alert = mon.CheckMass(new Tanker(), FuelType.Diesel, 20000);
        Assert.Equal(SafetyMonitor.Severity.Critical, alert?.Level);
    }

    [Fact]
    public void LossAboveNorm_Warns()
    {
        var mon = new SafetyMonitor();
        var order = new DeliveryOrder
        {
            Fuel = FuelType.Ai92,
            LoadedVolumeLiters = 2800,
            DeliveredVolumeLiters = 2700 // потеря 3.57% > нормы 0.9%
        };
        var alert = mon.CheckDeliveryLoss(order);
        Assert.Equal(SafetyMonitor.Severity.Warning, alert?.Level);
        Assert.False(order.IsLossWithinNorm);
    }
}

public class TripRepositoryTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"tanker_test_{Guid.NewGuid():N}.json");

    [Fact]
    public void Save_And_Reload_PreservesTrips()
    {
        var repo = new TripRepository(_file);
        var planner = new TripPlanner();
        var trip = planner.CreateTrip(DemoData.CreateTanker(), DemoData.Base(), DemoData.CreateOrders());
        repo.SaveTrip(trip);

        var repo2 = new TripRepository(_file);
        Assert.Single(repo2.Trips);
        Assert.Equal(trip.Id, repo2.CurrentTrip!.Id);
        Assert.Equal(trip.Orders.Count, repo2.CurrentTrip.Orders.Count);
    }

    [Fact]
    public void FinishTrip_ClearsCurrent()
    {
        var repo = new TripRepository(_file);
        var trip = new TripPlanner().CreateTrip(DemoData.CreateTanker(), DemoData.Base(), DemoData.CreateOrders());
        repo.SaveTrip(trip);
        repo.FinishTrip(trip.Id);
        Assert.Null(repo.CurrentTrip);
    }

    public void Dispose()
    {
        if (File.Exists(_file)) File.Delete(_file);
    }
}
