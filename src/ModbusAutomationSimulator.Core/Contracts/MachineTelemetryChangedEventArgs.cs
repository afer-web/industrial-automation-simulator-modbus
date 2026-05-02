namespace ModbusAutomationSimulator.Core.Contracts;

/// <summary>Notifica osservatori esterni (dashboard, SignalR) di un aggiornamento telemetrico.</summary>
public sealed class MachineTelemetryChangedEventArgs : EventArgs
{
    public MachineTelemetryChangedEventArgs(MachineSnapshot snapshot) => Snapshot = snapshot;

    public MachineSnapshot Snapshot { get; }
}
