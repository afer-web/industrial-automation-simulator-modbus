namespace ModbusAutomationSimulator.Core.Contracts;

/// <summary>
/// Fronti di comando digitali dal pannello/Modbus (start pulse, ecc.).</summary>
public readonly record struct MachineDigitalCommandEdges(
    bool StartPulse,
    bool FaultInjectPulse,
    bool AlarmAcknowledgePulse);
