using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TankerDriverApp.Core.Models;
using TankerDriverApp.Core.Services;

namespace TankerDriverApp.Mobile.ViewModels;

/// <summary>ViewModel экрана безопасности: тахограф, скорость, SOS.</summary>
public partial class SafetyViewModel : ObservableObject
{
    private readonly SafetyMonitor _monitor = new();

    [ObservableProperty] private double currentSpeedKmh;
    [ObservableProperty] private DateTime shiftStartUtc = DateTime.UtcNow.AddHours(-2);
    [ObservableProperty] private string drivingState = "За рулём";
    [ObservableProperty] private string sosMessage = "";

    public IReadOnlyList<SafetyMonitor.SafetyAlert> Alerts => _monitor.Alerts;

    public double DrivingHoursElapsed => Math.Round((DateTime.UtcNow - ShiftStartUtc).TotalHours, 1);
    public double BreakInHoursLeft => Math.Max(0, Math.Round(SafetyMonitor.MaxContinuousDrivingHours - DrivingHoursElapsed, 1));

    [RelayCommand]
    private void CheckNow()
    {
        _monitor.CheckDrivingSchedule(ShiftStartUtc, DateTime.UtcNow);
        var point = new TrackPoint
        {
            Position = new GeoPosition(55.75, 37.6, CurrentSpeedKmh, DateTime.UtcNow),
            EngineOn = DrivingState == "За рулём"
        };
        _monitor.CheckTrackPoint(point, null);
        OnPropertyChanged(nameof(Alerts));
        OnPropertyChanged(nameof(DrivingHoursElapsed));
        OnPropertyChanged(nameof(BreakInHoursLeft));
    }

    [RelayCommand]
    private void StartBreak()
    {
        DrivingState = "Отдых";
        ShiftStartUtc = DateTime.UtcNow;
        SosMessage = "Таймер вождения сброшен — отдых пошёл ⏱";
    }

    [RelayCommand]
    private void SendSos()
    {
        // В реальной сборке здесь: GPS-координаты + звонок диспетчеру + сигнал ГЛОНАСС-маяка
        _monitor.Report(SafetyMonitor.Severity.Critical, "SOS отправлен диспетчеру с координатами");
        SosMessage = "🆘 SOS отправлен! Диспетчер и служба безопасности уведомлены.";
        OnPropertyChanged(nameof(Alerts));
    }
}
