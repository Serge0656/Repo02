import { InputData, Trip, TripAssignment, Solution } from './types';

interface DeliveryTask {
  fuelType: string;
  stationIndex: number;
  volumeNeeded: number;
  volumeRemaining: number;
}

interface Slot {
  tankerId: number;
  compartmentIndex: number;
  capacity: number;
  speed: number;
  assigned: boolean;
}

interface SlotAssignment {
  slot: Slot;
  fuelType: string;
  stationIndex: number;
  volume: number;
}

interface ACOParams {
  numAnts: number;
  numIterations: number;
  alpha: number;
  beta: number;
  evaporation: number;
  Q: number;
}

export interface ACOConvergence {
  iteration: number;
  bestDistance: number;
  avgDistance: number;
}

const DEFAULT_ACO_PARAMS: ACOParams = {
  numAnts: 20,
  numIterations: 100,
  alpha: 1.0,
  beta: 2.0,
  evaporation: 0.5,
  Q: 100
};

export function solveOptimization(data: InputData, useACO: boolean = false): { solution: Solution; convergence?: ACOConvergence[] } {
  if (useACO) {
    return solveWithACO(data);
  }
  return { solution: solveWithBalancedGreedy(data) };
}

function solveWithBalancedGreedy(data: InputData): Solution {
  const { FuelTypes, Tankers, DistanceMatrix, StationDemand } = data;
  const numStations = 8;

  const slots: Slot[] = [];
  for (const tanker of Tankers) {
    for (let c = 0; c < tanker.Compartments.length; c++) {
      slots.push({
        tankerId: tanker.Id,
        compartmentIndex: c,
        capacity: tanker.Compartments[c].Capacity,
        speed: tanker.Speed,
        assigned: false
      });
    }
  }

  // Создаём все задачи доставки
  const allTasks: DeliveryTask[] = [];
  for (const ft of FuelTypes) {
    const demands = StationDemand[ft];
    for (let s = 0; s < numStations; s++) {
      if (demands[s] > 0) {
        allTasks.push({
          fuelType: ft,
          stationIndex: s,
          volumeNeeded: demands[s],
          volumeRemaining: demands[s]
        });
      }
    }
  }

  // Стратегия "Most Constrained First":
  // Считаем для каждой задачи количество подходящих секций
  // Задачи с меньшим количеством вариантов обрабатываем первыми
  const taskFlexibility = allTasks.map(task => {
    let flexibleSlots = 0;
    for (const slot of slots) {
      const minFill = slot.capacity * 0.8;
      // Секция подходит если: задача >= 80% секции ИЛИ секция <= задача
      if (task.volumeNeeded >= minFill || slot.capacity <= task.volumeNeeded) {
        flexibleSlots++;
      }
    }
    return { task, flexibility: flexibleSlots };
  });

  // Сортируем: наименее гибкие задачи первыми
  taskFlexibility.sort((a, b) => a.flexibility - b.flexibility);

  const assignments: SlotAssignment[] = [];

  // Обрабатываем задачи в порядке гибкости
  for (const { task } of taskFlexibility) {
    while (task.volumeRemaining > 0) {
      let bestSlot: Slot | null = null;
      let bestScore = Infinity;
      let bestVolume = 0;

      for (const slot of slots) {
        if (slot.assigned) continue;

        const minFill = slot.capacity * 0.8;
        let volumeToLoad: number;

        if (task.volumeRemaining >= slot.capacity) {
          volumeToLoad = slot.capacity;
        } else if (task.volumeRemaining >= minFill) {
          volumeToLoad = task.volumeRemaining;
        } else {
          volumeToLoad = slot.capacity;
        }

        const stationDist = DistanceMatrix[0][task.stationIndex + 1];
        const waste = Math.max(0, volumeToLoad - task.volumeRemaining);
        const sizePenalty = waste / slot.capacity;
        const score = sizePenalty * 100 + stationDist / slot.speed;

        if (score < bestScore) {
          bestScore = score;
          bestSlot = slot;
          bestVolume = volumeToLoad;
        }
      }

      if (bestSlot) {
        bestSlot.assigned = true;
        task.volumeRemaining -= bestVolume;

        assignments.push({
          slot: bestSlot,
          fuelType: task.fuelType,
          stationIndex: task.stationIndex,
          volume: bestVolume
        });
      } else {
        break;
      }
    }
  }

  return buildSolution(data, assignments);
}

function solveWithACO(data: InputData): { solution: Solution; convergence: ACOConvergence[] } {
  const { FuelTypes, Tankers, DistanceMatrix, StationDemand } = data;
  const numStations = 8;
  const params = DEFAULT_ACO_PARAMS;

  const tasks: DeliveryTask[] = [];
  for (const fuelType of FuelTypes) {
    const demands = StationDemand[fuelType];
    for (let s = 0; s < numStations; s++) {
      if (demands[s] > 0) {
        tasks.push({
          fuelType,
          stationIndex: s,
          volumeNeeded: demands[s],
          volumeRemaining: demands[s]
        });
      }
    }
  }

  const slots: Slot[] = [];
  for (const tanker of Tankers) {
    for (let c = 0; c < tanker.Compartments.length; c++) {
      slots.push({
        tankerId: tanker.Id,
        compartmentIndex: c,
        capacity: tanker.Compartments[c].Capacity,
        speed: tanker.Speed,
        assigned: false
      });
    }
  }

  tasks.sort((a, b) => b.volumeNeeded - a.volumeNeeded);

  const pheromones: number[][] = Array(9).fill(null).map(() => Array(9).fill(1.0));
  
  let bestSolution: SlotAssignment[] | null = null;
  let bestDistance = Infinity;
  const convergence: ACOConvergence[] = [];

  for (let iter = 0; iter < params.numIterations; iter++) {
    const antSolutions: SlotAssignment[][] = [];

    for (let ant = 0; ant < params.numAnts; ant++) {
      const antSlots = slots.map(s => ({ ...s, assigned: false }));
      const antTasks = tasks.map(t => ({ ...t, volumeRemaining: t.volumeNeeded }));
      const antAssignments: SlotAssignment[] = [];
      const antTankerStations = new Map<number, Set<number>>();

      for (const task of antTasks) {
        while (task.volumeRemaining > 0) {
          const candidates: { slot: Slot; volume: number; prob: number }[] = [];
          let totalProb = 0;

          for (const slot of antSlots) {
            if (slot.assigned) continue;

            const minFill = slot.capacity * 0.8;
            let volumeToLoad: number;

            if (task.volumeRemaining >= slot.capacity) {
              volumeToLoad = slot.capacity;
            } else if (task.volumeRemaining >= minFill) {
              volumeToLoad = task.volumeRemaining;
            } else {
              volumeToLoad = slot.capacity;
            }

            const stationDist = DistanceMatrix[0][task.stationIndex + 1];
            const heuristic = 1.0 / (stationDist + 1);
            
            let groupingFactor = 1.0;
            const existingStations = antTankerStations.get(slot.tankerId);
            if (existingStations && existingStations.has(task.stationIndex)) {
              groupingFactor = 2.0;
            }

            const pheromone = pheromones[0][task.stationIndex + 1];
            const prob = Math.pow(pheromone, params.alpha) * 
                        Math.pow(heuristic * groupingFactor, params.beta);

            candidates.push({ slot, volume: volumeToLoad, prob });
            totalProb += prob;
          }

          if (candidates.length === 0) break;

          const rand = Math.random() * totalProb;
          let cumulative = 0;
          let chosen = candidates[0];

          for (const cand of candidates) {
            cumulative += cand.prob;
            if (rand <= cumulative) {
              chosen = cand;
              break;
            }
          }

          chosen.slot.assigned = true;
          task.volumeRemaining -= chosen.volume;

          if (!antTankerStations.has(chosen.slot.tankerId)) {
            antTankerStations.set(chosen.slot.tankerId, new Set());
          }
          antTankerStations.get(chosen.slot.tankerId)!.add(task.stationIndex);

          antAssignments.push({
            slot: chosen.slot,
            fuelType: task.fuelType,
            stationIndex: task.stationIndex,
            volume: chosen.volume
          });
        }
      }

      antSolutions.push(antAssignments);
    }

    let iterBestSolution: SlotAssignment[] | null = null;
    let iterBestDistance = Infinity;
    let iterTotalDistance = 0;

    for (const solution of antSolutions) {
      const tempResult = buildSolution(data, solution);
      iterTotalDistance += tempResult.TotalDistance;
      if (tempResult.TotalDistance < iterBestDistance) {
        iterBestDistance = tempResult.TotalDistance;
        iterBestSolution = solution;
      }
    }

    for (let i = 0; i < 9; i++) {
      for (let j = 0; j < 9; j++) {
        pheromones[i][j] *= (1 - params.evaporation);
      }
    }

    if (iterBestSolution) {
      const tankerAssignments = new Map<number, Set<number>>();
      for (const a of iterBestSolution) {
        if (!tankerAssignments.has(a.slot.tankerId)) {
          tankerAssignments.set(a.slot.tankerId, new Set());
        }
        tankerAssignments.get(a.slot.tankerId)!.add(a.stationIndex);
      }

      for (const [, stations] of tankerAssignments) {
        const stationList = Array.from(stations);
        for (let i = 0; i < stationList.length; i++) {
          const from = i === 0 ? 0 : stationList[i - 1] + 1;
          const to = stationList[i] + 1;
          pheromones[from][to] += params.Q / iterBestDistance;
        }
        const lastStation = stationList[stationList.length - 1] + 1;
        pheromones[lastStation][0] += params.Q / iterBestDistance;
      }
    }

    if (iterBestDistance < bestDistance) {
      bestDistance = iterBestDistance;
      bestSolution = iterBestSolution;
    }

    convergence.push({
      iteration: iter + 1,
      bestDistance: bestDistance,
      avgDistance: iterTotalDistance / antSolutions.length
    });
  }

  return {
    solution: buildSolution(data, bestSolution || []),
    convergence
  };
}

function buildSolution(data: InputData, assignments: SlotAssignment[]): Solution {
  const { FuelTypes, Tankers, DistanceMatrix, StationDemand } = data;
  const numStations = 8;

  const tankerAssignments = new Map<number, SlotAssignment[]>();
  for (const a of assignments) {
    if (!tankerAssignments.has(a.slot.tankerId)) {
      tankerAssignments.set(a.slot.tankerId, []);
    }
    tankerAssignments.get(a.slot.tankerId)!.push(a);
  }

  const trips: Trip[] = [];

  for (const [tankerId, tankerAssigns] of tankerAssignments) {
    const tanker = Tankers.find(t => t.Id === tankerId)!;
    const stationsToVisit = [...new Set(tankerAssigns.map(a => a.stationIndex))];

    const route = stationsToVisit.length <= 1
      ? stationsToVisit
      : stationsToVisit.length <= 8
        ? bestPermutation(stationsToVisit, DistanceMatrix)
        : twoOpt(nearestNeighbor(stationsToVisit, DistanceMatrix), DistanceMatrix);

    let totalDistance = 0;
    let prevNode = 0;
    for (const station of route) {
      totalDistance += DistanceMatrix[prevNode][station + 1];
      prevNode = station + 1;
    }
    totalDistance += DistanceMatrix[prevNode][0];

    const totalTime = totalDistance / tanker.Speed;

    const tripAssignments: TripAssignment[] = tankerAssigns.map(a => ({
      TankerId: a.slot.tankerId,
      CompartmentIndex: a.slot.compartmentIndex,
      FuelType: a.fuelType,
      StationIndex: a.stationIndex,
      Volume: a.volume
    }));

    trips.push({
      TankerId: tankerId,
      Speed: tanker.Speed,
      Route: [0, ...route.map(s => s + 1), 0],
      Assignments: tripAssignments,
      TotalDistance: Math.round(totalDistance * 10) / 10,
      TotalTime: Math.round(totalTime * 100) / 100
    });
  }

  trips.sort((a, b) => a.TankerId - b.TankerId);

  const totalDistance = trips.reduce((sum, t) => sum + t.TotalDistance, 0);
  const totalTime = trips.reduce((sum, t) => sum + t.TotalTime, 0);

  const demandSatisfied: Record<string, number[]> = {};
  const demandTotal: Record<string, number[]> = {};

  for (const fuelType of FuelTypes) {
    demandSatisfied[fuelType] = new Array(numStations).fill(0);
    demandTotal[fuelType] = [...StationDemand[fuelType]];
  }

  for (const a of assignments) {
    demandSatisfied[a.fuelType][a.stationIndex] += a.volume;
  }

  let totalNeeded = 0;
  let totalSatisfied = 0;
  for (const fuelType of FuelTypes) {
    for (let s = 0; s < numStations; s++) {
      totalNeeded += demandTotal[fuelType][s];
      totalSatisfied += Math.min(demandSatisfied[fuelType][s], demandTotal[fuelType][s]);
    }
  }

  const satisfactionPercent = totalNeeded > 0 ? (totalSatisfied / totalNeeded) * 100 : 0;

  return {
    Trips: trips,
    TotalDistance: Math.round(totalDistance * 10) / 10,
    TotalTime: Math.round(totalTime * 100) / 100,
    DemandSatisfied: demandSatisfied,
    DemandTotal: demandTotal,
    SatisfactionPercent: Math.round(satisfactionPercent * 10) / 10
  };
}

function bestPermutation(stations: number[], dm: number[][]): number[] {
  if (stations.length <= 1) return stations;
  if (stations.length > 8) return twoOpt(nearestNeighbor(stations, dm), dm);
  
  const perms = permutations(stations);
  let best = stations;
  let bestDist = Infinity;

  for (const p of perms) {
    const d = calcRouteDist(p, dm);
    if (d < bestDist) {
      bestDist = d;
      best = p;
    }
  }
  return best;
}

function permutations(arr: number[]): number[][] {
  if (arr.length <= 1) return [arr];
  const result: number[][] = [];
  for (let i = 0; i < arr.length; i++) {
    const rest = [...arr.slice(0, i), ...arr.slice(i + 1)];
    for (const p of permutations(rest)) {
      result.push([arr[i], ...p]);
    }
  }
  return result;
}

function calcRouteDist(route: number[], dm: number[][]): number {
  let d = dm[0][route[0] + 1];
  for (let i = 0; i < route.length - 1; i++) {
    d += dm[route[i] + 1][route[i + 1] + 1];
  }
  d += dm[route[route.length - 1] + 1][0];
  return d;
}

function nearestNeighbor(stations: number[], dm: number[][]): number[] {
  const unvisited = [...stations];
  const route: number[] = [];
  let current = 0;

  while (unvisited.length > 0) {
    let nearest = -1;
    let nearestDist = Infinity;
    for (const s of unvisited) {
      const d = dm[current][s + 1];
      if (d < nearestDist) {
        nearestDist = d;
        nearest = s;
      }
    }
    route.push(nearest);
    current = nearest + 1;
    unvisited.splice(unvisited.indexOf(nearest), 1);
  }
  return route;
}

function twoOpt(route: number[], dm: number[][]): number[] {
  if (route.length <= 2) return route;
  let improved = true;
  let best = [...route];
  let bestDist = calcRouteDist(best, dm);

  while (improved) {
    improved = false;
    for (let i = 0; i < best.length - 1; i++) {
      for (let j = i + 1; j < best.length; j++) {
        const newRoute = [...best];
        const seg = newRoute.splice(i, j - i + 1);
        seg.reverse();
        newRoute.splice(i, 0, ...seg);
        const d = calcRouteDist(newRoute, dm);
        if (d < bestDist - 0.01) {
          best = newRoute;
          bestDist = d;
          improved = true;
        }
      }
    }
  }
  return best;
}

export function generateReportText(solution: Solution, data: InputData): string {
  let r = '';
  r += '══════════════════════════════════════════════════════════════════════════\n';
  r += '              ОТЧЁТ: ОПТИМИЗАЦИЯ МАРШРУТОВ БЕНЗОВОЗОВ\n';
  r += '══════════════════════════════════════════════════════════════════════════\n\n';
  r += `Сводка:\n`;
  r += `  Общий пробег: ${solution.TotalDistance} км\n`;
  r += `  Общее время: ${solution.TotalTime} ч\n`;
  r += `  Удовлетворение спроса: ${solution.SatisfactionPercent}%\n`;
  r += `  Количество рейсов: ${solution.Trips.length}\n\n`;
  r += '──────────────────────────────────────────────────────────────────────────\n';
  r += '                           ДЕТАЛИЗАЦИЯ РЕЙСОВ\n';
  r += '──────────────────────────────────────────────────────────────────────────\n\n';

  for (const trip of solution.Trips) {
    const tanker = data.Tankers.find(t => t.Id === trip.TankerId)!;
    r += `━━━ Бензовоз №${trip.TankerId} ━━━\n`;
    r += `  Скорость: ${tanker.Speed} км/ч\n`;
    r += `  Секции: ${tanker.Compartments.map((c, i) => `[${i + 1}]=${c.Capacity}л`).join(', ')}\n`;
    r += `  Маршрут: ${trip.Route.join(' → ')}\n`;
    r += `  Пробег: ${trip.TotalDistance} км\n`;
    r += `  Время: ${trip.TotalTime} ч\n`;
    r += `  Груз:\n`;
    for (const a of trip.Assignments) {
      const cap = tanker.Compartments[a.CompartmentIndex].Capacity;
      const pct = ((a.Volume / cap) * 100).toFixed(1);
      r += `    • Секция ${a.CompartmentIndex + 1} (${cap} л) → ${pct}% заполнена\n`;
      r += `      Топливо: ${a.FuelType} | Станция: ${a.StationIndex + 1} | Объём: ${a.Volume.toLocaleString()} л\n`;
    }
    r += '\n';
  }

  r += '──────────────────────────────────────────────────────────────────────────\n';
  r += '                      АНАЛИЗ УДОВЛЕТВОРЕНИЯ СПРОСА\n';
  r += '──────────────────────────────────────────────────────────────────────────\n\n';

  for (const ft of data.FuelTypes) {
    r += `  ⛽ ${ft}:\n`;
    for (let s = 0; s < 8; s++) {
      const need = solution.DemandTotal[ft][s];
      const got = solution.DemandSatisfied[ft][s];
      if (need > 0) {
        const pct = ((got / need) * 100).toFixed(1);
        const mark = got >= need ? '✓' : got > 0 ? '◐' : '✗';
        r += `    ${mark} Станция ${s + 1}: ${got.toLocaleString()} / ${need.toLocaleString()} л (${pct}%)\n`;
      }
    }
    r += '\n';
  }

  r += '══════════════════════════════════════════════════════════════════════════\n';
  return r;
}

export function generateCSV(solution: Solution, data: InputData): string {
  let csv = 'Бензовоз;Скорость;Секция;Топливо;Станция;Объём_л;Вместимость_л;Заполнение_%;Пробег_км;Время_ч\n';
  for (const trip of solution.Trips) {
    for (const a of trip.Assignments) {
      const tanker = data.Tankers.find(t => t.Id === trip.TankerId)!;
      const cap = tanker.Compartments[a.CompartmentIndex].Capacity;
      const pct = ((a.Volume / cap) * 100).toFixed(1);
      csv += `${trip.TankerId};${trip.Speed};${a.CompartmentIndex + 1};${a.FuelType};`;
      csv += `${a.StationIndex + 1};${a.Volume};${cap};${pct};${trip.TotalDistance};${trip.TotalTime}\n`;
    }
  }
  return csv;
}

export function generateDemandCSV(solution: Solution, data: InputData): string {
  let csv = 'Топливо;Станция;Потребность_л;Доставлено_л;Удовлетворение_%;Статус\n';
  for (const ft of data.FuelTypes) {
    for (let s = 0; s < 8; s++) {
      const need = solution.DemandTotal[ft][s];
      const got = solution.DemandSatisfied[ft][s];
      if (need > 0) {
        const pct = ((got / need) * 100).toFixed(1);
        const status = got >= need ? 'Полностью' : got > 0 ? 'Частично' : 'Не доставлено';
        csv += `${ft};${s + 1};${need};${got};${pct};${status}\n`;
      }
    }
  }
  return csv;
}
