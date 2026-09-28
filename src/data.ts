import { InputData } from './types';

export function initializeData(): InputData {
  return {
    FuelTypes: ["АИ-92", "АИ-95", "АИ-98", "ДТ"],
    
    Tankers: [
      { Id: 1, Speed: 50, Compartments: [{ Capacity: 22000 }] },
      { Id: 2, Speed: 50, Compartments: [{ Capacity: 22000 }] },
      { Id: 3, Speed: 50, Compartments: [{ Capacity: 22000 }] },
      { Id: 4, Speed: 50, Compartments: [{ Capacity: 17000 }] },
      { Id: 5, Speed: 55, Compartments: [{ Capacity: 11000 }, { Capacity: 9000 }] },
      { Id: 6, Speed: 55, Compartments: [{ Capacity: 11000 }, { Capacity: 9000 }] },
      { Id: 7, Speed: 55, Compartments: [{ Capacity: 11000 }, { Capacity: 9000 }] },
      { Id: 8, Speed: 55, Compartments: [{ Capacity: 11000 }, { Capacity: 9000 }] },
      { Id: 9, Speed: 55, Compartments: [{ Capacity: 8000 }, { Capacity: 7000 }] },
      { Id: 10, Speed: 60, Compartments: [{ Capacity: 7000 }, { Capacity: 5000 }, { Capacity: 4000 }] },
      { Id: 11, Speed: 59, Compartments: [{ Capacity: 7000 }, { Capacity: 5000 }, { Capacity: 4000 }] }
    ],
    
    DistanceMatrix: [
      [0,    3.7, 116, 108, 79,  34,   4.9,  28.6, 5.9 ],
      [3.7,  0,   113, 106, 76,  31,   4.3,  28,   56  ],
      [116,  113, 0,   190, 38,  144,  139,  135,  76  ],
      [108,  106, 190, 0,   181, 138,  110,  134,  160 ],
      [79,   76,  38,  181, 0,   101,  75,   98,   39  ],
      [34,   31,  144, 138, 101, 0,    28.3, 10.3, 79  ],
      [4.9,  4.3,  139, 110, 75,  28.3, 0,    25.4, 56  ],
      [28.6, 28,  135, 134, 98,  10.3, 25.4, 0,    79  ],
      [5.9,  56,  76,  160, 39,  79,   56,   79,   0   ]
    ],
    
    StationDemand: {
      "АИ-92": [27300, 0,     0,     20600, 24300, 12100, 0,     0    ],
      "АИ-95": [20800, 8100,  0,     21800, 0,     0,     0,     0    ],
      "АИ-98": [0,     0,     0,     0,     0,     0,     12400, 0    ],
      "ДТ":    [0,     12100, 12100, 12100, 0,     0,     8400,  12100]
    }
  };
}
