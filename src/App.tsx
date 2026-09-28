import { useState, useEffect } from 'react';
import { initializeData } from './data';
import { solveOptimization, generateReportText, generateCSV, generateDemandCSV } from './solver';
import { Solution, InputData } from './types';
import { csharpCode } from './csharpCode';

function App() {
  const [solution, setSolution] = useState<Solution | null>(null);
  const [data, setData] = useState<InputData | null>(null);
  const [activeTab, setActiveTab] = useState<'results' | 'trips' | 'demand' | 'map' | 'info' | 'code'>('results');
  const [algorithm, setAlgorithm] = useState<'greedy' | 'aco'>('aco');
  const [convergence, setConvergence] = useState<{ iteration: number; bestDistance: number; avgDistance: number }[]>([]);

  useEffect(() => {
    const inputData = initializeData();
    setData(inputData);
    const result = solveOptimization(inputData, algorithm === 'aco');
    setSolution(result.solution);
    setConvergence(result.convergence || []);
  }, [algorithm]);

  const downloadFile = (content: string, filename: string, mimeType: string) => {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleDownloadReport = () => {
    if (solution && data) {
      const report = generateReportText(solution, data);
      downloadFile(report, 'optimization_report.txt', 'text/plain;charset=utf-8');
    }
  };

  const handleDownloadCSV = () => {
    if (solution && data) {
      const csv = generateCSV(solution, data);
      downloadFile('\uFEFF' + csv, 'trips.csv', 'text/csv;charset=utf-8');
    }
  };

  const handleDownloadDemandCSV = () => {
    if (solution && data) {
      const csv = generateDemandCSV(solution, data);
      downloadFile('\uFEFF' + csv, 'demand_satisfaction.csv', 'text/csv;charset=utf-8');
    }
  };

  const handleDownloadCSharp = () => {
    downloadFile(csharpCode, 'TankerRouting.cs', 'text/plain;charset=utf-8');
  };

  if (!solution || !data) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-900">
        <div className="text-white text-xl">Загрузка...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 text-white">
      <header className="bg-gray-800/80 backdrop-blur-sm border-b border-gray-700 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 py-4">
          <div className="flex items-center justify-between flex-wrap gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex items-center justify-center">
                <span className="text-xl">🚛</span>
              </div>
              <div>
                <h1 className="text-xl font-bold">Оптимизация маршрутов бензовозов</h1>
                <p className="text-sm text-gray-400">Задача маршрутизации транспортных средств</p>
              </div>
            </div>
            <div className="flex gap-2 items-center flex-wrap">
              <div className="flex items-center gap-2 mr-4">
                <span className="text-sm text-gray-400">Алгоритм:</span>
                <select
                  value={algorithm}
                  onChange={(e) => setAlgorithm(e.target.value as 'greedy' | 'aco')}
                  className="px-3 py-2 bg-gray-700 border border-gray-600 rounded-lg text-sm text-white"
                >
                  <option value="greedy">Жадный</option>
                  <option value="aco">Муравьиная колония</option>
                </select>
              </div>
              <button onClick={handleDownloadReport} className="px-3 py-2 bg-green-600 hover:bg-green-700 rounded-lg text-sm font-medium transition-colors">
                📄 Отчёт
              </button>
              <button onClick={handleDownloadCSV} className="px-3 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg text-sm font-medium transition-colors">
                📊 Рейсы
              </button>
              <button onClick={handleDownloadDemandCSV} className="px-3 py-2 bg-purple-600 hover:bg-purple-700 rounded-lg text-sm font-medium transition-colors">
                📋 Спрос
              </button>
              <button onClick={handleDownloadCSharp} className="px-3 py-2 bg-indigo-600 hover:bg-indigo-700 rounded-lg text-sm font-medium transition-colors">
                💾 C# код
              </button>
            </div>
          </div>
        </div>
      </header>

      <div className="max-w-7xl mx-auto px-4 py-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-gray-700">
            <div className="text-sm text-gray-400 mb-1">Общий пробег</div>
            <div className="text-2xl font-bold text-blue-400">{solution.TotalDistance} <span className="text-sm font-normal">км</span></div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-gray-700">
            <div className="text-sm text-gray-400 mb-1">Общее время</div>
            <div className="text-2xl font-bold text-green-400">{solution.TotalTime} <span className="text-sm font-normal">ч</span></div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-gray-700">
            <div className="text-sm text-gray-400 mb-1">Удовлетворение спроса</div>
            <div className="text-2xl font-bold text-yellow-400">{solution.SatisfactionPercent}%</div>
          </div>
          <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-4 border border-gray-700">
            <div className="text-sm text-gray-400 mb-1">Активных рейсов</div>
            <div className="text-2xl font-bold text-purple-400">{solution.Trips.length}</div>
          </div>
        </div>

        <div className="flex gap-1 mb-4 bg-gray-800/50 rounded-lg p-1 w-fit flex-wrap">
          {(['results', 'trips', 'demand', 'map', 'info', 'code'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-4 py-2 rounded-md text-sm font-medium transition-all ${
                activeTab === tab
                  ? 'bg-blue-600 text-white shadow-lg'
                  : 'text-gray-400 hover:text-white hover:bg-gray-700'
              }`}
            >
              {tab === 'results' && '📋 Результаты'}
              {tab === 'trips' && '🚛 Рейсы'}
              {tab === 'demand' && '📊 Спрос'}
              {tab === 'map' && '🗺️ Схема'}
              {tab === 'info' && 'ℹ️ Данные'}
              {tab === 'code' && '💻 C# код'}
            </button>
          ))}
        </div>

        <div className="bg-gray-800/50 backdrop-blur-sm rounded-xl border border-gray-700 overflow-hidden">
          {activeTab === 'results' && <ResultsTab solution={solution} data={data} algorithm={algorithm} />}
          {activeTab === 'trips' && <TripsTab solution={solution} data={data} />}
          {activeTab === 'demand' && <DemandTab solution={solution} data={data} />}
          {activeTab === 'map' && <MapTab data={data} solution={solution} convergence={convergence} algorithm={algorithm} />}
          {activeTab === 'info' && <InfoTab data={data} />}
          {activeTab === 'code' && <CodeTab />}
        </div>
      </div>
    </div>
  );
}

function ResultsTab({ solution, data, algorithm }: { solution: Solution; data: InputData; algorithm: string }) {
  const report = generateReportText(solution, data);

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-bold">📋 Результаты работы алгоритма</h2>
        <span className="px-3 py-1 bg-blue-600/20 text-blue-300 rounded-full text-sm">
          {algorithm === 'aco' ? '🐜 Муравьиная колония' : '⚡ Жадный алгоритм'}
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-gradient-to-br from-blue-600/20 to-blue-800/20 rounded-xl p-4 border border-blue-500/30">
          <div className="text-sm text-blue-300 mb-1">Общий пробег</div>
          <div className="text-3xl font-bold text-white">{solution.TotalDistance}</div>
          <div className="text-xs text-gray-400 mt-1">километров</div>
        </div>
        <div className="bg-gradient-to-br from-green-600/20 to-green-800/20 rounded-xl p-4 border border-green-500/30">
          <div className="text-sm text-green-300 mb-1">Общее время</div>
          <div className="text-3xl font-bold text-white">{solution.TotalTime}</div>
          <div className="text-xs text-gray-400 mt-1">часов</div>
        </div>
        <div className="bg-gradient-to-br from-purple-600/20 to-purple-800/20 rounded-xl p-4 border border-purple-500/30">
          <div className="text-sm text-purple-300 mb-1">Удовлетворение спроса</div>
          <div className="text-3xl font-bold text-white">{solution.SatisfactionPercent}%</div>
          <div className="text-xs text-gray-400 mt-1">
            {solution.SatisfactionPercent === 100 ? '✓ Полностью' : '△ Частично'}
          </div>
        </div>
        <div className="bg-gradient-to-br from-orange-600/20 to-orange-800/20 rounded-xl p-4 border border-orange-500/30">
          <div className="text-sm text-orange-300 mb-1">Активных рейсов</div>
          <div className="text-3xl font-bold text-white">{solution.Trips.length}</div>
          <div className="text-xs text-gray-400 mt-1">из {data.Tankers.length} бензовозов</div>
        </div>
      </div>

      <div className="bg-gray-900 rounded-lg border border-gray-600 overflow-hidden">
        <div className="bg-gray-700 px-4 py-2 text-xs text-gray-300 flex items-center gap-2">
          <span className="w-3 h-3 rounded-full bg-red-500"></span>
          <span className="w-3 h-3 rounded-full bg-yellow-500"></span>
          <span className="w-3 h-3 rounded-full bg-green-500"></span>
          <span className="ml-2">optimization_report.txt</span>
        </div>
        <pre className="p-4 overflow-x-auto text-xs text-gray-300 leading-relaxed max-h-[600px] overflow-y-auto font-mono">
          {report}
        </pre>
      </div>
    </div>
  );
}

function TripsTab({ solution, data }: { solution: Solution; data: InputData }) {
  return (
    <div className="p-6">
      <h2 className="text-lg font-bold mb-4">Рейсы бензовозов ({solution.Trips.length})</h2>
      <div className="space-y-4">
        {solution.Trips.map(trip => {
          const tanker = data.Tankers.find(t => t.Id === trip.TankerId)!;
          return (
            <div key={trip.TankerId} className="bg-gray-900/50 rounded-lg p-4 border border-gray-600">
              <div className="flex items-center justify-between mb-3">
                <div className="flex items-center gap-3">
                  <span className="text-2xl">🚛</span>
                  <div>
                    <span className="font-bold text-lg">Бензовоз №{trip.TankerId}</span>
                    <span className="text-gray-400 ml-2 text-sm">({tanker.Speed} км/ч, {tanker.Compartments.length} секц.)</span>
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-blue-400 font-bold">{trip.TotalDistance} км</div>
                  <div className="text-gray-400 text-sm">{trip.TotalTime} ч</div>
                </div>
              </div>
              
              <div className="mb-3">
                <span className="text-xs text-gray-500 uppercase tracking-wide">Маршрут:</span>
                <div className="flex items-center gap-1 mt-1 flex-wrap">
                  {trip.Route.map((node, i) => (
                    <span key={i} className="flex items-center gap-1">
                      <span className={`px-2 py-1 rounded text-xs font-medium ${
                        node === 0 ? 'bg-orange-600/30 text-orange-300' : 'bg-blue-600/30 text-blue-300'
                      }`}>
                        {node === 0 ? '🏭 База' : `Ст. ${node}`}
                      </span>
                      {i < trip.Route.length - 1 && <span className="text-gray-500">→</span>}
                    </span>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                {trip.Assignments.map((a, idx) => {
                  const capacity = tanker.Compartments[a.CompartmentIndex].Capacity;
                  const fillPct = ((a.Volume / capacity) * 100).toFixed(1);
                  return (
                    <div key={idx} className="bg-gray-800 rounded p-2 text-sm">
                      <div className="flex justify-between items-center">
                        <span className="font-medium">
                          Секция {a.CompartmentIndex + 1} ({capacity} л)
                        </span>
                        <span className={`px-2 py-0.5 rounded text-xs ${getFuelColor(a.FuelType)}`}>
                          {a.FuelType}
                        </span>
                      </div>
                      <div className="text-gray-400 mt-1">
                        → Станция {a.StationIndex + 1}: {a.Volume.toLocaleString()} л ({fillPct}%)
                      </div>
                      <div className="mt-1 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                        <div 
                          className="h-full bg-gradient-to-r from-blue-500 to-green-500 rounded-full"
                          style={{ width: `${fillPct}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function DemandTab({ solution, data }: { solution: Solution; data: InputData }) {
  return (
    <div className="p-6">
      <h2 className="text-lg font-bold mb-4">Удовлетворение спроса по станциям</h2>
      <div className="space-y-6">
        {data.FuelTypes.map(fuelType => (
          <div key={fuelType} className="bg-gray-900/50 rounded-lg p-4 border border-gray-600">
            <h3 className="font-bold mb-3 flex items-center gap-2">
              <span className={`w-3 h-3 rounded-full ${getFuelDotColor(fuelType)}`}></span>
              {fuelType}
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {solution.DemandTotal[fuelType].map((needed, s) => {
                if (needed === 0) return null;
                const delivered = solution.DemandSatisfied[fuelType][s];
                const pct = Math.min(100, (delivered / needed) * 100);
                return (
                  <div key={s} className="bg-gray-800 rounded p-3">
                    <div className="flex justify-between items-center mb-2">
                      <span className="font-medium text-sm">Станция {s + 1}</span>
                      <span className={`text-sm font-bold ${pct >= 100 ? 'text-green-400' : pct >= 80 ? 'text-yellow-400' : 'text-red-400'}`}>
                        {pct.toFixed(1)}%
                      </span>
                    </div>
                    <div className="text-xs text-gray-400 mb-1">
                      {delivered.toLocaleString()} / {needed.toLocaleString()} л
                    </div>
                    <div className="h-2 bg-gray-700 rounded-full overflow-hidden">
                      <div 
                        className={`h-full rounded-full transition-all ${
                          pct >= 100 ? 'bg-green-500' : pct >= 80 ? 'bg-yellow-500' : 'bg-red-500'
                        }`}
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function MapTab({ data, solution, convergence, algorithm }: { data: InputData; solution: Solution; convergence: { iteration: number; bestDistance: number; avgDistance: number }[]; algorithm: string }) {
  const stationPositions = [
    { x: 50, y: 50, label: '🏭 База' },
    { x: 48, y: 30, label: 'Ст. 1' },
    { x: 85, y: 20, label: 'Ст. 2' },
    { x: 80, y: 75, label: 'Ст. 3' },
    { x: 65, y: 55, label: 'Ст. 4' },
    { x: 35, y: 65, label: 'Ст. 5' },
    { x: 30, y: 35, label: 'Ст. 6' },
    { x: 55, y: 25, label: 'Ст. 7' },
    { x: 42, y: 75, label: 'Ст. 8' }
  ];

  const colors = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16', '#f97316', '#6366f1', '#14b8a6'];

  return (
    <div className="p-6">
      <h2 className="text-lg font-bold mb-4">Схема маршрутов</h2>
      <div className="relative bg-gray-900 rounded-lg border border-gray-600 overflow-hidden" style={{ height: '500px' }}>
        <svg width="100%" height="100%" viewBox="0 0 100 100" preserveAspectRatio="xMidYMid meet">
          {solution.Trips.map((trip, tripIdx) => {
            const color = colors[tripIdx % colors.length];
            const points = trip.Route.map(node => {
              const pos = stationPositions[node];
              return `${pos.x},${pos.y}`;
            }).join(' ');
            return (
              <polyline
                key={tripIdx}
                points={points}
                fill="none"
                stroke={color}
                strokeWidth="0.3"
                strokeOpacity="0.6"
                strokeDasharray="1,0.5"
              />
            );
          })}
          
          {stationPositions.map((pos, i) => (
            <g key={i}>
              <circle cx={pos.x} cy={pos.y} r={i === 0 ? 3 : 2} fill={i === 0 ? '#f97316' : '#3b82f6'} opacity="0.8" />
              <text x={pos.x} y={pos.y - 4} textAnchor="middle" fontSize="2.5" fill="white" fontWeight="bold">
                {pos.label}
              </text>
            </g>
          ))}
        </svg>
      </div>

      {algorithm === 'aco' && convergence.length > 0 && (
        <div className="mt-6">
          <h3 className="font-bold mb-3">📈 Сходимость муравьиного алгоритма</h3>
          <div className="bg-gray-900 rounded-lg border border-gray-600 p-4">
            <div className="mt-3 text-sm text-gray-400">
              <div>Параметры ACO: муравьёв = 20, итераций = 100, α = 1.0, β = 2.0, испарение = 0.5</div>
              <div className="mt-1">Начальный пробег: <span className="text-yellow-400">{convergence[0]?.avgDistance.toFixed(1)} км</span> → Лучший: <span className="text-blue-400">{convergence[convergence.length - 1]?.bestDistance.toFixed(1)} км</span></div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function InfoTab({ data }: { data: InputData }) {
  return (
    <div className="p-6">
      <h2 className="text-lg font-bold mb-4">Входные данные</h2>
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-gray-900/50 rounded-lg p-4 border border-gray-600">
          <h3 className="font-bold mb-3">🚛 Бензовозы ({data.Tankers.length})</h3>
          <div className="space-y-2">
            {data.Tankers.map(tanker => (
              <div key={tanker.Id} className="flex items-center justify-between bg-gray-800 rounded p-2 text-sm">
                <span className="font-medium">№{tanker.Id}</span>
                <span className="text-gray-400">{tanker.Speed} км/ч</span>
                <div className="flex gap-1">
                  {tanker.Compartments.map((c, i) => (
                    <span key={i} className="px-2 py-0.5 bg-blue-600/30 text-blue-300 rounded text-xs">
                      {(c.Capacity / 1000).toFixed(0)}т
                    </span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-gray-900/50 rounded-lg p-4 border border-gray-600">
          <h3 className="font-bold mb-3">⛽ Потребность станций</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-gray-600">
                  <th className="p-2 text-left text-gray-400">Топливо</th>
                  {[1,2,3,4,5,6,7,8].map(s => (
                    <th key={s} className="p-2 text-center text-gray-400">Ст.{s}</th>
                  ))}
                  <th className="p-2 text-center text-gray-400">Σ</th>
                </tr>
              </thead>
              <tbody>
                {data.FuelTypes.map(fuel => {
                  const demands = data.StationDemand[fuel];
                  const total = demands.reduce((s, v) => s + v, 0);
                  return (
                    <tr key={fuel} className="border-b border-gray-700">
                      <td className="p-2">
                        <span className={`px-2 py-0.5 rounded text-xs ${getFuelColor(fuel)}`}>{fuel}</span>
                      </td>
                      {demands.map((d, i) => (
                        <td key={i} className="p-2 text-center text-gray-300">
                          {d > 0 ? (d / 1000).toFixed(1) + 'т' : '-'}
                        </td>
                      ))}
                      <td className="p-2 text-center font-bold text-white">
                        {(total / 1000).toFixed(1)}т
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}

function CodeTab() {
  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-bold">Исходный код на C#</h2>
        <button
          onClick={() => {
            const blob = new Blob([csharpCode], { type: 'text/plain;charset=utf-8' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = 'TankerRouting.cs';
            a.click();
            URL.revokeObjectURL(url);
          }}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 rounded-lg text-sm font-medium transition-colors"
        >
          💾 Скачать TankerRouting.cs
        </button>
      </div>
      <div className="bg-gray-900 rounded-lg border border-gray-600 overflow-hidden">
        <div className="bg-gray-700 px-4 py-2 text-xs text-gray-300 flex items-center gap-2">
          <span className="w-3 h-3 rounded-full bg-red-500"></span>
          <span className="w-3 h-3 rounded-full bg-yellow-500"></span>
          <span className="w-3 h-3 rounded-full bg-green-500"></span>
          <span className="ml-2">TankerRouting.cs</span>
        </div>
        <pre className="p-4 overflow-x-auto text-xs text-gray-300 leading-relaxed max-h-[600px] overflow-y-auto">
          <code>{csharpCode}</code>
        </pre>
      </div>
    </div>
  );
}

function getFuelColor(fuel: string): string {
  switch (fuel) {
    case 'АИ-92': return 'bg-blue-600/30 text-blue-300';
    case 'АИ-95': return 'bg-green-600/30 text-green-300';
    case 'АИ-98': return 'bg-purple-600/30 text-purple-300';
    case 'ДТ': return 'bg-yellow-600/30 text-yellow-300';
    default: return 'bg-gray-600/30 text-gray-300';
  }
}

function getFuelDotColor(fuel: string): string {
  switch (fuel) {
    case 'АИ-92': return 'bg-blue-400';
    case 'АИ-95': return 'bg-green-400';
    case 'АИ-98': return 'bg-purple-400';
    case 'ДТ': return 'bg-yellow-400';
    default: return 'bg-gray-400';
  }
}

export default App;
