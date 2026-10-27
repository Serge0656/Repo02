using TankerDriverApp.Core.Models;

namespace TankerDriverApp.Core.Services;

/// <summary>Геодетические расчёты (формула гаверсинуса) и оценка времени в пути.</summary>
public static class GeoCalculator
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>Расстояние по прямой между двумя точками, км.</summary>
    public static double HaversineKm(GeoPosition a, GeoPosition b) =>
        HaversineKm(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    /// <summary>
    /// Приблизительное расстояние по автодорогам: коэффициент извилистости
    /// (detour factor) — реально дорога длиннее прямой на 20–40%.
    /// </summary>
    public static double RoadDistanceKm(Station from, Station to, double detourFactor = 1.3) =>
        Math.Round(HaversineKm(from.Latitude, from.Longitude, to.Latitude, to.Longitude) * detourFactor, 1);

    /// <summary>Оценка времени в пути, мин (средняя скорость бензовоза ограничена).</summary>
    public static double EstimateTimeMin(double distanceKm, double averageSpeedKmh)
    {
        if (averageSpeedKmh <= 0) averageSpeedKmh = 55;
        // Бензовоз — опасный груз: тахограф + средний город/трасса ~55 км/ч
        return Math.Round(distanceKm / averageSpeedKmh * 60, 0);
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
