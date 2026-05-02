namespace ModbusAutomationSimulator.Core.Simulation;

/// <summary>
/// Stato sintetizzato degli ingressi discreti (PLC → campo reale).</summary>
public readonly record struct DiscreteSensorState(
    bool ConveyorMotorRunning,
    bool PieceAtConveyorEntry,
    bool PieceAtStation,
    bool StationClampEngaged,
    bool QualityStationActive,
    bool WarehouseOutboundReady);
