# Beverage Filling Line Server

A complete OPC UA server implementation for a beverage filling line simulation. This application provides real-time process data, machine control methods, and comprehensive alarm management for industrial automation systems.

## Overview

This OPC UA server simulates a **FluidFill Express #2** beverage filling line with realistic production behavior, including:
- Real-time process variable simulation
- Industrial-grade alarm system
- Complete machine control via OPC UA methods
- Production order management
- Quality control monitoring

## Getting Started

### Prerequisites
- .NET 8.0 Runtime
- Visual Studio 2022 or VS Code
- UaExpert (for OPC UA client testing)

### Building and Running
```bash
dotnet build
dotnet run
```

The server will start on `opc.tcp://localhost:4840`

## UaExpert Setup

### Connection Configuration
1. **Open UaExpert**
2. **Add Server** → **Add** → **Advanced**
3. **Enter Details:**
   - **Name:** `Beverage Filling Line Server`
   - **Endpoint URL:** `opc.tcp://localhost:4840`
4. **Click OK** and **Connect**

### Browsing Data
Navigate to: `Objects → BeverageFillingLine`
- **Variables:** All machine parameters and status
- **Control Methods:** Machine control functions (in "Control Methods" folder)
- **Alarms:** ActiveAlarms array and AlarmCount

## OPC UA Variables (42 Total)

### Machine Identification (5 Variables)
| Variable | Data Type | Description | Example |
|----------|-----------|-------------|---------|
| `MachineName` | String | Machine identifier | "FluidFill Express #2" |
| `MachineSerialNumber` | String | Serial number | "FFE2000-2023-002" |
| `Plant` | String | Production facility | "Dortmund Beverage Center" |
| `ProductionSegment` | String | Production segment | "Non-Alcoholic Beverages" |
| `ProductionLine` | String | Production line | "Juice Filling Line 3" |

### Production Order (5 Variables)
| Variable | Data Type | Description | Example |
|----------|-----------|-------------|---------|
| `ProductionOrder` | String | Current order number | "PO-2024-JUICE-5567" |
| `Article` | String | Product article code | "ART-JUICE-APPLE-1L" |
| `Quantity` | UInt32 | Order quantity | 25000 |
| `CurrentLotNumber` | String | Current batch | "LOT-2024-APPLE-0456" |
| `ExpirationDate` | DateTime | Product expiration | 2026-09-23 |

### Target Values (6 Variables)
| Variable | Data Type | Unit | Description | Typical Range |
|----------|-----------|------|-------------|---------------|
| `TargetFillVolume` | Double | ml | Target fill volume | 1000.0 |
| `TargetLineSpeed` | Double | BPM | Target bottles per minute | 450.0 |
| `TargetProductTemperature` | Double | °C | Target product temperature | 6.5 |
| `TargetCO2Pressure` | Double | bar | Target CO2 pressure | 3.8 |
| `TargetCapTorque` | Double | Nm | Target cap torque | 22.0 |
| `TargetCycleTime` | Double | s | Target cycle time | 2.67 |

### Actual Values (7 Variables)
| Variable | Data Type | Unit | Description | Updates |
|----------|-----------|------|-------------|---------|
| `ActualFillVolume` | Double | ml | Current fill volume | Real-time |
| `ActualLineSpeed` | Double | BPM | Current line speed | Real-time |
| `ActualProductTemperature` | Double | °C | Current temperature | Real-time |
| `ActualCO2Pressure` | Double | bar | Current CO2 pressure | Real-time |
| `ActualCapTorque` | Double | Nm | Current cap torque | Real-time |
| `ActualCycleTime` | Double | s | Current cycle time | Real-time |
| `FillAccuracyDeviation` | Double | ml | Fill deviation (Actual - Target) | Calculated |

### System Status (6 Variables)
| Variable | Data Type | Description | Possible Values |
|----------|-----------|-------------|-----------------|
| `MachineStatus` | String | Machine operating state | Running, Stopped, Starting, Stopping, Maintenance, Error |
| `CurrentStation` | String | Active production station | Station 1-16 |
| `ProductLevelTank` | Double | Tank level percentage | 0-100% |
| `CleaningCycleStatus` | String | Cleaning system status | Normal Production, CIP Active, SIP Active, Sanitizing |
| `QualityCheckWeight` | String | Weight check result | Pass, Fail |
| `QualityCheckLevel` | String | Level check result | Pass, Fail |

### Counters - Global (7 Variables)
| Variable | Data Type | Description | Resets |
|----------|-----------|-------------|---------|
| `GoodBottles` | UInt32 | Total good bottles since startup | Manual only |
| `BadBottlesVolume` | UInt32 | Bad bottles due to volume | Manual only |
| `BadBottlesWeight` | UInt32 | Bad bottles due to weight | Manual only |
| `BadBottlesCap` | UInt32 | Bad bottles due to cap issues | Manual only |
| `BadBottlesOther` | UInt32 | Bad bottles due to other reasons | Manual only |
| `TotalBadBottles` | UInt32 | Total bad bottles (calculated) | Calculated |
| `TotalBottles` | UInt32 | Total bottles produced (calculated) | Calculated |

### Counters - Current Order (4 Variables)
| Variable | Data Type | Description | Resets |
|----------|-----------|-------------|---------|
| `GoodBottlesOrder` | UInt32 | Good bottles in current order | New order |
| `BadBottlesOrder` | UInt32 | Bad bottles in current order | New order |
| `TotalBottlesOrder` | UInt32 | Total bottles in current order | Calculated |
| `ProductionOrderProgress` | Double | Order completion percentage | Calculated |

### Alarms (2 Variables)
| Variable | Data Type | Description | Updates |
|----------|-----------|-------------|---------|
| `ActiveAlarms` | String[] | Array of active alarm messages | Real-time |
| `AlarmCount` | UInt32 | Number of active alarms | Real-time |

## OPC UA Methods (11 Total)

### Basic Machine Control (4 Methods)
| Method | Parameters | Description | Effect |
|--------|------------|-------------|---------|
| `StartMachine` | None | Starts the filling machine | Status: Stopped → Starting → Running |
| `StopMachine` | None | Stops the filling machine | Status: Running → Stopping → Stopped |
| `EmergencyStop` | None | Emergency stop activation | Status: → Error, Adds alarm |
| `EnterMaintenanceMode` | None | Enters maintenance mode | Status: → Maintenance |

### Cleaning Operations (2 Methods)
| Method | Parameters | Description | Duration |
|--------|------------|-------------|----------|
| `StartCIPCycle` | None | Clean-in-Place cycle | 60 seconds |
| `StartSIPCycle` | None | Sterilize-in-Place cycle | 90 seconds |

### Production Control (3 Methods)
| Method | Parameters | Description | Validation |
|--------|------------|-------------|------------|
| `AdjustFillVolume` | `newFillVolume` (Double) | Adjusts target fill volume | ±5% of current |
| `ResetCounters` | None | Resets all production counters | Immediate |
| `GenerateLotNumber` | None | Generates new lot number | Format: LOT-YYYY-ART-MMDDHH |

### Advanced Operations (2 Methods)
| Method | Parameters | Description |
|--------|------------|-------------|
| `LoadProductionOrder` | 9 parameters | Loads new production order |
| `ChangeProduct` | 4 parameters | Changes product specifications |

#### LoadProductionOrder Parameters
1. `orderNumber` (String) - Production order number
2. `article` (String) - Article code
3. `quantity` (UInt32) - Order quantity
4. `targetFillVolume` (Double) - Target fill volume
5. `targetLineSpeed` (Double) - Target line speed
6. `targetProductTemp` (Double) - Target temperature
7. `targetCO2Pressure` (Double) - Target CO2 pressure
8. `targetCapTorque` (Double) - Target cap torque
9. `targetCycleTime` (Double) - Target cycle time

#### ChangeProduct Parameters
1. `newArticle` (String) - New article code
2. `newTargetFillVolume` (Double) - New fill volume
3. `newTargetProductTemp` (Double) - New temperature
4. `newTargetCO2Pressure` (Double) - New CO2 pressure

## Alarm System

### Alarm Types and Thresholds

#### Fill Volume Alarms
- **1% Deviation:** Fill volume deviates ±1% from target
- **Example:** `"ALARM: Fill deviation 12.5ml exceeds ±1%"`

#### Temperature Alarms
- **±2°C Threshold:** Product temperature exceeds ±2°C from target
- **Example:** `"ALARM: Product temperature 8.7°C exceeds ±2°C"`

#### Pressure Alarms
- **±0.2 bar Threshold:** CO2 pressure deviates more than ±0.2 bar
- **Example:** `"ALARM: CO2 pressure 4.05 bar deviates more than ±0.2 bar"`

#### Level Alarms
- **15% Threshold:** Tank level drops below 15%
- **Example:** `"ALARM: Tank level 12.3% too low (< 15%)"`

#### Cap Torque Alarms
- **±10% Threshold:** Cap torque outside ±10% range
- **Example:** `"ALARM: Cap torque 25.2 Nm outside ±10% range"`

#### Process Deviation Alarms
- **3% for 3 cycles:** Consistent deviation over multiple cycles
- **8% single cycle:** Severe single-cycle deviation
- **Example:** `"ALARM: FillVolume deviation > 3% for 3 cycles. Values: 1032.4, 1031.8, 1033.1"`

#### Emergency Alarms
- **Emergency Stop:** `"EMERGENCY STOP ACTIVATED - Manual intervention required"`

### Alarm Monitoring
- **Real-time updates:** Every 3 seconds
- **Historical tracking:** Last 10 cycles per parameter
- **Automatic clearing:** When conditions return to normal
- **Manual clearing:** Not available (realistic industrial behavior)

## Simulation Behavior

### Variable Updates
- **Update frequency:** Every 3 seconds
- **Realistic variations:** Random deviations within industrial tolerances
- **Tank depletion:** Gradual tank level decrease (0.01% per cycle)
- **Station cycling:** Rotates through 16 production stations
- **Bottle counting:** Adds bottles every 3 cycles, bad bottles every 15 cycles

### Machine States
```
Stopped → StartMachine() → Starting (3s) → Running
Running → StopMachine() → Stopping (3s) → Stopped
Any State → EmergencyStop() → Error
Any State → EnterMaintenanceMode() → Maintenance
```

### Cleaning Cycles
```
CIP: Maintenance/Stopped → CIP Active (60s) → Normal Production
SIP: Maintenance/Stopped → SIP Active (90s) → Normal Production
```

## Architecture

### Class Structure
```
Program.cs
├── BeverageFillingLineServer.cs (OPC UA Server)
│   └── BeverageFillingLineNodeManager.cs (Address Space)
│       └── BeverageFillingLineMachine.cs (Simulation Logic)
```

### Key Components
- **OPC UA Server:** StandardServer implementation with certificate handling
- **Node Manager:** CustomNodeManager2 for address space creation
- **Machine Simulation:** Complete beverage line simulation with realistic behavior
- **Variable Updates:** Timer-based real-time data updates
- **Method Handlers:** OPC UA method execution with parameter validation

## Troubleshooting

### Common Issues

#### Server Won't Start
```
Error: "Need at least one Application Certificate"
```
**Solution:** Certificate directories are created automatically. Restart the application.

#### UaExpert Can't Connect
```
Error: "BadTcpInternalError"
```
**Solutions:**
1. Run as Administrator
2. Check Windows Firewall settings
3. Verify port 4840 is available
4. Try endpoint `opc.tcp://127.0.0.1:4840`

#### Methods Not Visible
**Solutions:**
1. Expand "Control Methods" folder
2. Refresh address space (F5)
3. Restart UaExpert

### Verification Steps
1. **Server Console:** Should show "Server started at: opc.tcp://localhost:4840"
2. **UaExpert Connection:** Green connection status
3. **Variables:** All 42 variables visible and updating
4. **Methods:** All 11 methods in "Control Methods" folder

## Development Notes

### Code Conventions
- **Modern C# naming:** Private fields use `_` prefix
- **Async/await patterns:** Proper async handling throughout
- **Error handling:** Comprehensive try-catch blocks
- **Clean code:** Minimal console output, focused on OPC UA interface

### Performance
- **Update frequency:** 3-second intervals for optimal balance
- **Memory management:** Proper disposal patterns
- **Thread safety:** Lock-based synchronization for OPC UA variables