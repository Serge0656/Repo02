using TankerDriverApp.Core.Models;

namespace TankerDriverApp.Core.Services;

/// <summary>Демо-данные для тестов и превью приложения (реальные приходят с сервера логистики).</summary>
public static class DemoData
{
    public static Station Base() => new()
    {
        Id = 0, Name = "НПЗ «Нефтехим» — наливная эстакада", Latitude = 55.7558, Longitude = 37.6173, IsBase = true
    };

    public static Tanker CreateTanker() => new()
    {
        PlateNumber = "А123ВС777",
        Model = "КамАЗ-65115 / цистерна 12 м³ (4 секции)",
        SpeedKmh = 55,
        OdometerKm = 187_420,
        Compartments = new List<Compartment>
        {
            new() { Index = 1, CapacityLiters = 3000 },
            new() { Index = 2, CapacityLiters = 3000 },
            new() { Index = 3, CapacityLiters = 4000 }, // ДТ — секция увеличенного объёма
            new() { Index = 4, CapacityLiters = 3000 },
        }
    };

    public static List<DeliveryOrder> CreateOrders()
    {
        var now = DateTime.UtcNow;
        return new List<DeliveryOrder>
        {
            new()
            {
                Fuel = FuelType.Ai92, RequestedVolumeLiters = 2800,
                Destination = new Station { Id = 1, Name = "АЗС №14, Тверская", Latitude = 55.7648, Longitude = 37.6053 },
                PlannedDepartureUtc = now, PlannedArrivalUtc = now.AddMinutes(40), PriceRub = 21000,
                Comment = "Пломбы №№ 44501–44503"
            },
            new()
            {
                Fuel = FuelType.Ai95, RequestedVolumeLiters = 2900,
                Destination = new Station { Id = 2, Name = "АЗС №7, Кутузовский пр-т", Latitude = 55.7412, Longitude = 37.5340 },
                PlannedDepartureUtc = now, PlannedArrivalUtc = now.AddMinutes(60), PriceRub = 24500,
                Comment = "Приёмщик — старший смены Иванов"
            },
            new()
            {
                Fuel = FuelType.Diesel, RequestedVolumeLiters = 3000,
                Destination = new Station { Id = 3, Name = "Автобаза «Юг», Домодедово", Latitude = 55.4375, Longitude = 37.7442 },
                PlannedDepartureUtc = now, PlannedArrivalUtc = now.AddMinutes(90), PriceRub = 27000,
                Comment = "Слив через pump-метр, нужен акт"
            },
            new()
            {
                Fuel = FuelType.Ai92, RequestedVolumeLiters = 2500,
                Destination = new Station { Id = 4, Name = "АЗС №31, Мытищи", Latitude = 55.9116, Longitude = 37.7361 },
                PlannedDepartureUtc = now, PlannedArrivalUtc = now.AddMinutes(75), PriceRub = 19800,
                Comment = ""
            },
        };
    }
}
