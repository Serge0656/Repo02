export interface Compartment {
  Capacity: number;
}

export interface Tanker {
  Id: number;
  Speed: number;
  Compartments: Compartment[];
}

export interface InputData {
  FuelTypes: string[];
  Tankers: Tanker[];
  DistanceMatrix: number[][];
  StationDemand: Record<string, number[]>;
}

export interface TripAssignment {
  TankerId: number;
  CompartmentIndex: number;
  FuelType: string;
  StationIndex: number;
  Volume: number;
}

export interface Trip {
  TankerId: number;
  Speed: number;
  Route: number[];
  Assignments: TripAssignment[];
  TotalDistance: number;
  TotalTime: number;
}

export interface Solution {
  Trips: Trip[];
  TotalDistance: number;
  TotalTime: number;
  DemandSatisfied: Record<string, number[]>;
  DemandTotal: Record<string, number[]>;
  SatisfactionPercent: number;
}
