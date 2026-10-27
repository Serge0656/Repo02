using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TankerDriverApp.Core.Models;
using TankerDriverApp.Core.Services;

namespace TankerDriverApp.Mobile.ViewModels;

/// <summary>ViewModel экрана выгрузки на АЗС: учёт принятого объёма и потерь.</summary>
public partial class DeliveryViewModel : ObservableObject
{
    private readonly SafetyMonitor _safety = new();

    [ObservableProperty] private DeliveryOrder? order;
    [ObservableProperty] private double acceptedLiters;
    [ObservableProperty] private string message = "Выберите заявку для выгрузки";
    [ObservableProperty] private bool hasPhotoProof;

    public double LossPercent => Order is { LoadedVolumeLiters: > 0 } && AcceptedLiters > 0
        ? Math.Round((Order.LoadedVolumeLiters - AcceptedLiters) / Order.LoadedVolumeLiters * 100, 2)
        : 0;

    partial void OnOrderChanged(DeliveryOrder? value)
    {
        if (value is not null)
        {
            AcceptedLiters = value.RequestedVolumeLiters;
            Message = $"{value.Fuel.Display()}: загружено {value.LoadedVolumeLiters:F0} л. " +
                      $"Норма потерь ≤ {value.AllowedLossPercent}%.";
        }
        OnPropertyChanged(nameof(LossPercent));
    }

    partial void OnAcceptedLitersChanged(double value) => OnPropertyChanged(nameof(LossPercent));

    [RelayCommand]
    private void TakePhoto()
    {
        HasPhotoProof = true;
        Message += "\n📸 Фото пломбы и pump-метра сохранено в чек-лист.";
    }

    [RelayCommand]
    private void ConfirmDelivery()
    {
        if (Order is null) return;
        Order.DeliveredVolumeLiters = AcceptedLiters;
        var alert = _safety.CheckDeliveryLoss(Order);
        if (alert is not null)
        {
            Message = alert.Message + (HasPhotoProof ? " Акт с фото приложен." : " Сделайте фото и оформите акт!");
            return;
        }
        Order.Status = OrderStatus.Completed;
        Order.ActualArrivalUtc = DateTime.UtcNow;
        Message = $"Выгрузка подтверждена: {AcceptedLiters:F0} л принято, потери в норме ✅";
    }
}
