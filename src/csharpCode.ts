export const csharpCode = `using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TankerRouting
{
    public class Compartment
    {
        public double Capacity { get; set; }
    }

    public class Tanker
    {
        public int Id { get; set; }
        public double Speed { get; set; }
        public List<Compartment> Compartments { get; set; } = new();
    }

    public class InputData
    {
        public List<string> FuelTypes { get; set; }
        public List<Tanker> Tankers { get; set; }
        public double[,] DistanceMatrix { get; set; }
        public Dictionary<string, double[]> StationDemand { get; set; }
    }

    public class TripAssignment
    {
        public int TankerId { get; set; }
        public int CompartmentIndex { get; set; }
        public string FuelType { get; set; }
        public int StationIndex { get; set; }
        public double Volume { get; set; }
    }

    public class Trip
    {
        public int TankerId { get; set; }
        public double Speed { get; set; }
        public List<int> Route { get; set; } = new();
        public List<TripAssignment> Assignments { get; set; } = new();
        public double TotalDistance { get; set; }
        public double TotalTime { get; set; }
    }

    public class Solution
    {
        public List<Trip> Trips { get; set; } = new();
        public double TotalDistance { get; set; }
        public double TotalTime { get; set; }
        public Dictionary<string, double[]> DemandSatisfied { get; set; }
        public Dictionary<string, double[]> DemandTotal { get; set; }
        public double SatisfactionPercent { get; set; }
    }

    class Program
    {
        static InputData InitializeData()
        {
            return new InputData
            {
                FuelTypes = new List<string> { "АИ-92", "АИ-95", "АИ-98", "ДТ" },
                Tankers = new List<Tanker>
                {
                    new Tanker { Id = 1, Speed = 50, Compartments = new List<Compartment> { new Compartment { Capacity = 22000 } } },
                    new Tanker { Id = 2, Speed = 50, Compartments = new List<Compartment> { new Compartment { Capacity = 22000 } } },
                    new Tanker { Id = 3, Speed = 50, Compartments = new List<Compartment> { new Compartment { Capacity = 22000 } } },
                    new Tanker { Id = 4, Speed = 50, Compartments = new List<Compartment> { new Compartment { Capacity = 17000 } } },
                    new Tanker { Id = 5, Speed = 55, Compartments = new List<Compartment> { new Compartment { Capacity = 11000 }, new Compartment { Capacity = 9000 } } },
                    new Tanker { Id = 6, Speed = 55, Compartments = new List<Compartment> { new Compartment { Capacity = 11000 }, new Compartment { Capacity = 9000 } } },
                    new Tanker { Id = 7, Speed = 55, Compartments = new List<Compartment> { new Compartment { Capacity = 11000 }, new Compartment { Capacity = 9000 } } },
                    new Tanker { Id = 8, Speed = 55, Compartments = new List<Compartment> { new Compartment { Capacity = 11000 }, new Compartment { Capacity = 9000 } } },
                    new Tanker { Id = 9, Speed = 55, Compartments = new List<Compartment> { new Compartment { Capacity = 8000 }, new Compartment { Capacity = 7000 } } },
                    new Tanker { Id = 10, Speed = 60, Compartments = new List<Compartment> { new Compartment { Capacity = 7000 }, new Compartment { Capacity = 5000 }, new Compartment { Capacity = 4000 } } },
                    new Tanker { Id = 11, Speed = 59, Compartments = new List<Compartment> { new Compartment { Capacity = 7000 }, new Compartment { Capacity = 5000 }, new Compartment { Capacity = 4000 } } }
                },
                DistanceMatrix = new double[9, 9]
                {
                    { 0, 3.7, 116, 108, 79, 34, 4.9, 28.6, 5.9 },
                    { 3.7, 0, 113, 106, 76, 31, 4.3, 28, 56 },
                    { 116, 113, 0, 190, 38, 144, 139, 135, 76 },
                    { 108, 106, 190, 0, 181, 138, 110, 134, 160 },
                    { 79, 76, 38, 181, 0, 101, 75, 98, 39 },
                    { 34, 31, 144, 138, 101, 0, 28.3, 10.3, 79 },
                    { 4.9, 4.3, 139, 110, 75, 28.3, 0, 25.4, 56 },
                    { 28.6, 28, 135, 134, 98, 10.3, 25.4, 0, 79 },
                    { 5.9, 56, 76, 160, 39, 79, 56, 79, 0 }
                },
                StationDemand = new Dictionary<string, double[]>
                {
                    { "АИ-92", new double[] { 27300, 0, 0, 20600, 24300, 12100, 0, 0 } },
                    { "АИ-95", new double[] { 20800, 8100, 0, 21800, 0, 0, 0, 0 } },
                    { "АИ-98", new double[] { 0, 0, 0, 0, 0, 0, 12400, 0 } },
                    { "ДТ", new double[] { 0, 12100, 12100, 12100, 0, 0, 8400, 12100 } }
                }
            };
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var data = InitializeData();
            Console.WriteLine("Проект готов к запуску!");
            Console.WriteLine("Общая потребность: " + data.StationDemand.Values.Sum(d => d.Sum()) + " л");
        }
    }
}
`;
