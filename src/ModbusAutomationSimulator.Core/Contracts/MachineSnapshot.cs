using ModbusAutomationSimulator.Core.Domain;
using ModbusAutomationSimulator.Core.Domain.Enumerations;
using ModbusAutomationSimulator.Core.Simulation;

namespace ModbusAutomationSimulator.Core.Contracts;

/// <summary>
/// Istantanea coerente dello stato cella verso UI/Modbus (thread-safe tramite orchestratore).</summary>
public readonly record struct MachineSnapshot(
    MachineState MachineState,
    ushort ActiveAlarmRegister,
    DiscreteSensorState DiscreteSensors,
    AnalogSensorState AnalogSensors,
    int GoodPiecesCount,
    int ScrapPiecesCount,
    ushort PhaseElapsedMs,
    ushort TotalCycleElapsedMs,
    bool IsRunningAutomatic,
    bool FaultInjectLatch,
    ushort StatusWordBits);
