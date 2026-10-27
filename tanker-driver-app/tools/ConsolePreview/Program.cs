using TankerDriverApp.Core.Models;
using TankerDriverApp.Core.Services;

// ─── Консольное превью бизнес-логики приложения «Бензовоз · Водитель» ───
// Позволяет проверить работу ядра (Core) без сборки MAUI: 
// dotnet run --project tools/ConsolePreview

var baseStation = DemoData.Base();
var tanker = DemoData.CreateTanker();
var orders = DemoData.CreateOrders();
var planner = new TripPlanner();
var safety = new SafetyMonitor();

Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine("║   🚛 БЕНЗОВОЗ · ВОДИТЕЛЬ — демо рейса        ║");
Console.WriteLine("╚══════════════════════════════════════════════╝");
Console.WriteLine($"База: {baseStation.Name}");
Console.WriteLine($"ТС:   {tanker.Model} ({tanker.PlateNumber}), секций: {tanker.Compartments.Count}\n");

// 1. Создание рейса: загрузка по секциям + построение маршрута
var trip = planner.CreateTrip(tanker, baseStation, orders);

Console.WriteLine("─── Загрузка по секциям ───");
foreach (var c in tanker.Compartments)
    Console.WriteLine($"  Секция {c.Index}: {c.CurrentVolumeLiters,6:F0} л / {c.CapacityLiters} л " +
                      $"({c.FillPercent}% ) — {c.Fuel?.Display() ?? "пусто"}");

Console.WriteLine("\n─── Маршрут (ближайший сосед от базы) ───");
for (var i = 0; i < trip.Legs.Count; i++)
{
    var leg = trip.Legs[i];
    Console.WriteLine($"  {i + 1}. {leg.To.Name,-28} {leg.DistanceKm,6} км  ~{leg.EstimatedTimeMin,3} мин  " +
                      $"{leg.VolumeLiters,5:F0} л {leg.Fuel.Display()}");
}
Console.WriteLine($"  ИТОГО: {trip.TotalDistanceKm:F1} км, ~{trip.TotalEstimatedTimeMin:F0} мин в пути\n");

// 2. Симуляция deliveries: на каждой точке принимаем объём с небольшой потерей
Console.WriteLine("─── Выгрузки ───");
foreach (var order in trip.Orders)
{
    order.Status = OrderStatus.Delivering;
    // приёмка: недолив в пределах/сверх нормы для демонстрации контроля
    var acceptedRatio = order.Fuel == FuelType.Diesel ? 0.978 : 0.995;
    order.DeliveredVolumeLiters = Math.Round(order.LoadedVolumeLiters * acceptedRatio, 0);
    order.ActualArrivalUtc = DateTime.UtcNow;

    var lossAlert = safety.CheckDeliveryLoss(order);
    order.Status = OrderStatus.Completed;

    Console.WriteLine($"  {order.Id} → {order.Destination.Name}");
    Console.WriteLine($"    загружено {order.LoadedVolumeLiters:F0} л, принято {order.DeliveredVolumeLiters:F0} л, " +
                      $"потери {order.LossPercent}% (норма ≤{order.AllowedLossPercent}%) " +
                      $"{(lossAlert is null ? "✅ в норме" : "⚠️ " + lossAlert.Message)}");
}

// 3. Проверки безопасности
Console.WriteLine("\n─── Безопасность ───");
var speedPoint = new TrackPoint { Position = new GeoPosition(55.8, 37.7, 88, DateTime.UtcNow), EngineOn = true };
safety.CheckTrackPoint(speedPoint, null);
safety.CheckDrivingSchedule(DateTime.UtcNow.AddHours(-4.6), DateTime.UtcNow);
foreach (var a in safety.Alerts)
    Console.WriteLine($"  [{a.Level}] {a.Message}");

// 4. Сохранение в офлайн-репозиторий
var repoPath = Path.Combine(Path.GetTempPath(), "tanker_demo_trip.json");
if (File.Exists(repoPath)) File.Delete(repoPath);
var repo = new TripRepository(repoPath);
repo.SaveTrip(trip);
Console.WriteLine($"\nРейс сохранён в офлайн-кэш: {repoPath} ({new FileInfo(repoPath).Length} байт)");
Console.WriteLine($"Выручка смены: {trip.TotalRevenueRub:N0} ₽");
Console.WriteLine("Смена завершена ✔");
