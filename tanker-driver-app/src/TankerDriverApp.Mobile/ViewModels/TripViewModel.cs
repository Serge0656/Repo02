using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TankerDriverApp.Core.Models;
using TankerDriverApp.Core.Services;

namespace TankerDriverApp.Mobile.ViewModels;

/// <summary>Главный ViewModel: текущий рейс, статусы заявок, переходы по этапам.</summary>
public partial class TripViewModel : ObservableObject
{
    private readonly TripRepository _repository = new();
    private readonly TripPlanner _planner = new();

    [ObservableProperty] private Trip? currentTrip;
    [ObservableProperty] private DeliveryOrder? selectedOrder;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = "Смена не начата";

    public Tanker Tanker { get; } = DemoData.CreateTanker();
    public Station Base => DemoData.Base();

    public double TotalDistanceKm => CurrentTrip?.TotalDistanceKm ?? 0;
    public int ActiveOrdersCount => CurrentTrip?.ActiveOrders.Count() ?? 0;

    partial void OnCurrentTripChanged(Trip? value)
    {
        OnPropertyChanged(nameof(TotalDistanceKm));
        OnPropertyChanged(nameof(ActiveOrdersCount));
    }

    [RelayCommand]
    private async Task StartShiftAsync()
    {
        IsBusy = true;
        try
        {
            await Task.Delay(300); // имитация запроса к серверу логистики
            var trip = _planner.CreateTrip(Tanker, Base, DemoData.CreateOrders());
            _repository.SaveTrip(trip);
            CurrentTrip = trip;
            StatusMessage = $"Рейс {trip.Id}: загружено {Tanker.CurrentVolumeLiters:F0} л, маршрут {trip.TotalDistanceKm:F1} км";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = "Ошибка: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void MarkArrived(DeliveryOrder? order)
    {
        if (order is null) return;
        order.Status = OrderStatus.Delivering;
        var leg = CurrentTrip?.Legs.FirstOrDefault(l => l.To.Id == order.Destination.Id);
        if (leg is not null) leg.Arrived = true;
        StatusMessage = $"Прибытие: {order.Destination.Name}. Выполните выгрузку.";
        Refresh();
    }

    [RelayCommand]
    private void CompleteOrder(DeliveryOrder? order)
    {
        if (order is null || CurrentTrip is null) return;
        order.ActualArrivalUtc = DateTime.UtcNow;
        order.Status = OrderStatus.Completed;
        TripPlanner.Unload(Tanker, order.Fuel, order.DeliveredVolumeLiters);
        _repository.SaveTrip(CurrentTrip);
        StatusMessage = $"Заявка {order.Id} закрыта. Потери: {order.LossPercent}%";
        Refresh();
    }

    [RelayCommand]
    private async Task FinishShiftAsync()
    {
        if (CurrentTrip is null) return;
        var active = ActiveOrdersCount;
        if (active > 0)
        {
            StatusMessage = $"Нельзя закрыть смену: {active} активных заявок";
            return;
        }
        _repository.FinishTrip(CurrentTrip.Id);
        CurrentTrip = null;
        StatusMessage = "Смена завершена. Данные отправлены диспетчеру.";
        await Task.CompletedTask;
    }

    private void Refresh()
    {
        if (CurrentTrip is not null) _repository.SaveTrip(CurrentTrip);
        OnPropertyChanged(nameof(ActiveOrdersCount));
    }
}
