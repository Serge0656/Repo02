namespace TankerDriverApp.Core.Models;

/// <summary>Тип топлива, перевозимого бензовозом.</summary>
public enum FuelType
{
    /// <summary>Бензин АИ-92</summary>
    Ai92,
    /// <summary>Бензин АИ-95</summary>
    Ai95,
    /// <summary>Бензин АИ-98</summary>
    Ai98,
    /// <summary>Дизельное топливо</summary>
    Diesel,
    /// <summary>Авиакеросин</summary>
    JetFuel,
    /// <summary>Мазут</summary>
    Mazut
}

public static class FuelTypeExtensions
{
    /// <summary>Плотность топлива, кг/л (для контроля массы загрузки).</summary>
    public static double Density(this FuelType fuel) => fuel switch
    {
        FuelType.Ai92 => 0.731,
        FuelType.Ai95 => 0.742,
        FuelType.Ai98 => 0.763,
        FuelType.Diesel => 0.840,
        FuelType.JetFuel => 0.790,
        FuelType.Mazut => 0.930,
        _ => 0.75
    };

    public static string Display(this FuelType fuel) => fuel switch
    {
        FuelType.Ai92 => "АИ-92",
        FuelType.Ai95 => "АИ-95",
        FuelType.Ai98 => "АИ-98",
        FuelType.Diesel => "ДТ",
        FuelType.JetFuel => "ТС-1",
        FuelType.Mazut => "Мазут",
        _ => fuel.ToString()
    };
}
