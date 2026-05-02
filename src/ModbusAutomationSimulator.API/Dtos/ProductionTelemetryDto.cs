using ModbusAutomationSimulator.Core.Contracts;

namespace ModbusAutomationSimulator.API.Dtos;

/// <summary>DTO JSON-ready per REST/SignalR derivato dall'istantanea cella.</summary>
public sealed record ProductionTelemetryDto(
    ushort MachineState,
    ushort AlarmCode,
    bool ConveyorMotor,
    bool PieceAtEntry,
    bool PieceAtStation,
    bool QualityMeasuring,
    bool OutboundLaneReady,
    int GoodPieces,
    int ScrapPieces,
    ushort PhaseElapsedMs,
    ushort TotalCycleElapsedMs,
    bool AutoCycleEngaged,
    ushort StatusBits);

internal static class ProductionTelemetryMapper
{
    internal static ProductionTelemetryDto From(MachineSnapshot s) =>
        new(
            (ushort)s.MachineState,
            s.ActiveAlarmRegister,
            s.DiscreteSensors.ConveyorMotorRunning,
            s.DiscreteSensors.PieceAtConveyorEntry,
            s.DiscreteSensors.PieceAtStation,
            s.DiscreteSensors.QualityStationActive,
            s.DiscreteSensors.WarehouseOutboundReady,
            s.GoodPiecesCount,
            s.ScrapPiecesCount,
            s.PhaseElapsedMs,
            s.TotalCycleElapsedMs,
            s.IsRunningAutomatic,
            s.StatusWordBits);
}
