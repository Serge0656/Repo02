using TankerDriverApp.Core.Models;

namespace TankerDriverApp.Core.Services;

/// <summary>
/// Контроль безопасности перевозок опасного груза (ADR/ДОПОГ):
/// ограничения скорости, режим труда и отдыха по тахографу,
/// допустимые потери топлива, суммарная масса.
/// </summary>
public class SafetyMonitor
{
    /// <summary>Максимальная скорость бензовоза с опасным грузом (ограничитель), км/ч.</summary>
    public const double MaxSpeedKmh = 80;

    /// <summary>Предупреждение о превышении, км/ч.</summary>
    public const double SpeedWarningKmh = 75;

    /// <summary>Максимум непрерывного вождения по тахографу, ч.</summary>
    public const double MaxContinuousDrivingHours = 4.5;

    /// <summary>Обязательный перерыв, мин.</summary>
    public const double RequiredBreakMin = 45;

    public record SafetyAlert(Severity Level, string Message);

    public enum Severity { Info, Warning, Critical }

    private readonly List<SafetyAlert> _alerts = new();
    public IReadOnlyList<SafetyAlert> Alerts => _alerts;

    /// <summary>Проверка телеметрической точки: скорость, перегрев, отклонение от маршрута.</summary>
    public SafetyAlert? CheckTrackPoint(TrackPoint point, GeoPosition? plannedNext)
    {
        if (point.EngineOn && point.Position.SpeedKmh > MaxSpeedKmh)
            return Alert(Severity.Critical,
                $"Превышение скорости! {point.Position.SpeedKmh:F0} км/ч (лимит {MaxSpeedKmh} км/ч, ограничитель ДОПОГ)");

        if (point.EngineOn && point.Position.SpeedKmh > SpeedWarningKmh)
            return Alert(Severity.Warning,
                $"Приближение к лимиту скорости: {point.Position.SpeedKmh:F0} км/ч");

        if (plannedNext is not null)
        {
            var deviation = GeoCalculator.HaversineKm(point.Position, plannedNext);
            if (deviation > 5)
                return Alert(Severity.Warning, $"Отклонение от маршрута {deviation:F1} км — проверьте навигацию");
        }
        return null;
    }

    /// <summary>Проверка режима работы по тахографу.</summary>
    public SafetyAlert? CheckDrivingSchedule(DateTime shiftStartUtc, DateTime nowUtc)
    {
        var hours = (nowUtc - shiftStartUtc).TotalHours;
        if (hours >= MaxContinuousDrivingHours)
            return Alert(Severity.Critical,
                $"Непрерывное вождение {hours:F1} ч — обязателен перерыв {RequiredBreakMin} мин (тахограф)");
        if (hours >= MaxContinuousDrivingHours - 0.5)
            return Alert(Severity.Warning,
                $"Через {MaxContinuousDrivingHours - hours:F1} ч — пора на перерыв согласно тахографу");
        return null;
    }

    /// <summary>Проверка суммарной массы загрузки (ОММ тягача + цистерна ≈ 25 т в РФ).</summary>
    public SafetyAlert? CheckMass(Tanker tanker, FuelType fuel, double liters)
    {
        const double TareTons = 9.5;      // масса порожней машины-цистерны
        const double MaxGrossTons = 25.0; // допустимая полная масса
        var cargoTons = liters * fuel.Density() / 1000.0;
        var gross = TareTons + cargoTons;
        if (gross > MaxGrossTons)
            return Alert(Severity.Critical,
                $"Перегруз! Полная масса {gross:F1} т > {MaxGrossTons} т. Снизьте загрузку на {(gross - MaxGrossTons) / fuel.Density() * 1000:F0} л");
        if (gross > MaxGrossTons - 1)
            return Alert(Severity.Warning, $"Полная масса {gross:F1} т близка к пределу {MaxGrossTons} т");
        return null;
    }

    /// <summary>Проверка потерь при выгрузке относительно норм (ГОСТ Р 8.589).</summary>
    public SafetyAlert? CheckDeliveryLoss(DeliveryOrder order)
    {
        if (order.LossPercent > order.AllowedLossPercent)
            return Alert(Severity.Warning,
                $"{order.Id}: фактические потери {order.LossPercent}% превышают норму {order.AllowedLossPercent}%. " +
                "Зафиксируйте акт, проверьте пломбы и работу pump-метра.");
        if (order.LossPercent < 0)
            return Alert(Severity.Critical,
                $"{order.Id}: приёмник принял БОЛЬШЕ, чем загружено ({-order.LossPercent}%). Проверьте сливочные документы.");
        return null;
    }

    /// <summary>Публичная регистрация произвольного оповещения (например, SOS).</summary>
    public SafetyAlert Report(Severity level, string message) => Alert(level, message);

    private SafetyAlert Alert(Severity level, string message)
    {
        _alerts.Add(new SafetyAlert(level, message));
        return _alerts[^1];
    }

    public void Clear() => _alerts.Clear();
}
