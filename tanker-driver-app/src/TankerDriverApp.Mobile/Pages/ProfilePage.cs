using TankerDriverApp.Mobile.ViewModels;

namespace TankerDriverApp.Mobile.Pages;

/// <summary>Профиль водителя и ТС: данные, документы, наряды.</summary>
public class ProfilePage : ContentPage
{
    public ProfilePage(TripViewModel vm)
    {
        Title = "Профиль";

        var t = vm.Tanker;
        var info = new Label
        {
            FontSize = 14,
            Text =
                $"👤 Водитель: Петров С. А.\n" +
                $"Категория CE, ДОПОГ до 12.2027\n" +
                $"Тахографная карта: RU-ТК-448192\n\n" +
                $"🚛 ТС: {t.Model}\n" +
                $"Гос. номер: {t.PlateNumber}\n" +
                $"Пробег: {t.OdometerKm:N0} км\n" +
                $"Вместимость: {t.CapacityLiters / 1000:F1} м³ ({t.Compartments.Count} секции)\n\n" +
                $"📄 Документы: СТС, полис ОСАГО/ОГПС, путевой лист — действительны"
        };

        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 20, Children = { info } } };
    }
}
