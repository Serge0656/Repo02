using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TankerDriverApp.Core.Models;
using TankerDriverApp.Core.Services;

namespace TankerDriverApp.Mobile.ViewModels;

/// <summary>ViewModel экрана загрузки на эстакаде НПЗ.</summary>
public partial class LoadViewModel : ObservableObject
{
    private readonly TripPlanner _planner = new();
    private readonly SafetyMonitor _safety = new();

    [ObservableProperty] private FuelType selectedFuel = FuelType.Ai92;
    [ObservableProperty] private double plannedLiters = 2800;
    [ObservableProperty] private double factLiters;
    [ObservableProperty] private string message = "Установите секции и откройте налив";
    [ObservableProperty] private bool canStartLoad = true;

    public Tanker Tanker { get; set; } = DemoData.CreateTanker();
    public IReadOnlyList<FuelType> Fuels { get; } = new[]
    {
        FuelType.Ai92, FuelType.Ai95, FuelType.Ai98, FuelType.Diesel, FuelType.JetFuel, FuelType.Mazut
    };

    public double EstimatedMassTons => Math.Round(PlannedLiters * SelectedFuel.Density() / 1000.0, 2);

    partial void OnSelectedFuelChanged(FuelType value)
    {
        OnPropertyChanged(nameof(EstimatedMassTons));
        CheckMass();
    }
    partial void OnPlannedLitersChanged(double value)
    {
        OnPropertyChanged(nameof(EstimatedMassTons));
        CheckMass();
    }

    private void CheckMass()
    {
        var alert = _safety.CheckMass(Tanker, SelectedFuel, PlannedLiters);
        CanStartLoad = alert is null || alert.Level != SafetyMonitor.Severity.Critical;
        if (alert is not null) Message = alert.Message;
    }

    [RelayCommand]
    private void StartLoading()
    {
        if (!TripPlanner.CanLoad(Tanker, SelectedFuel, PlannedLiters))
        {
            Message = $"Нет свободных секций для {PlannedLiters} л {SelectedFuel.Display()}";
            return;
        }
        FactLiters = TripPlanner.LoadCompartments(Tanker, SelectedFuel, PlannedLiters);
        Message = $"Налив завершён: {FactLiters:F0} л {SelectedFuel.Display()} " +
                  $"(~{EstimatedMassTons} т). Проверьте пломбы.";
    }

    [RelayCommand]
    private void EmergencyStop()
    {
        foreach (var c in Tanker.Compartments)
        {
            c.CurrentVolumeLiters = 0;
            c.Fuel = null;
        }
        FactLiters = 0;
        Message = "АВАРИЙНЫЙ СЛИВ выполнен. Немедленно сообщите диспетчеру и по ОПС!";
    }
}
