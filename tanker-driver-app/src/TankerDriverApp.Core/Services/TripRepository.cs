using Newtonsoft.Json;
using TankerDriverApp.Core.Models;

namespace TankerDriverApp.Core.Services;

/// <summary>Хранилище рейсов и заявок: локальный офлайн-кэш (JSON-файл) + синхронизация с сервером.</summary>
public class TripRepository
{
    private readonly string _filePath;
    private readonly List<Trip> _trips = new();

    public event EventHandler? DataChanged;

    public TripRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TankerDriverApp", "trips.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        Load();
    }

    public IReadOnlyList<Trip> Trips => _trips;

    public Trip? CurrentTrip => _trips.LastOrDefault(t => t.FinishedUtc is null);

    public void SaveTrip(Trip trip)
    {
        var idx = _trips.FindIndex(t => t.Id == trip.Id);
        if (idx >= 0) _trips[idx] = trip; else _trips.Add(trip);
        Persist();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void FinishTrip(string tripId)
    {
        var trip = _trips.FirstOrDefault(t => t.Id == tripId);
        if (trip is null) return;
        trip.FinishedUtc = DateTime.UtcNow;
        Persist();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<DeliveryOrder> HistoryOrders() =>
        _trips.SelectMany(t => t.Orders).OrderByDescending(o => o.PlannedDepartureUtc);

    private void Load()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            var json = File.ReadAllText(_filePath);
            _trips.AddRange(JsonConvert.DeserializeObject<List<Trip>>(json) ?? new());
        }
        catch (JsonException)
        {
            // повреждённый кэш — начинаем с чистого листа, файл перезапишется при первом сохранении
            _trips.Clear();
        }
    }

    private void Persist()
    {
        var json = JsonConvert.SerializeObject(_trips, Formatting.Indented);
        File.WriteAllText(_filePath, json);
    }
}
