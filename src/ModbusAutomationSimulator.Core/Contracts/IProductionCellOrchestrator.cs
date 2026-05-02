using ModbusAutomationSimulator.Core.Domain;

namespace ModbusAutomationSimulator.Core.Contracts;

/// <summary>
/// Facciata dell'orchestratore: ciclo vita comandi macchina + tick simulatore.</summary>
public interface IProductionCellOrchestrator
{
    /// <summary>Iscrizione cambi stato per UI/notifiche.</summary>
    event EventHandler<MachineTelemetryChangedEventArgs>? TelemetryChanged;

    MachineSnapshot Snapshot { get; }

    void Tick(TimeSpan delta);

    void RequestStartAutomatic();

    /// <summary>Arresto fine ciclo: completa caricamento/QC/scarico del pezzo corrente, poi resta in Idle senza ricaricare.</summary>
    void RequestStopAutomatic();

    void RequestEmergencyStop();

    /// <summary>Reset fault/E-Stop dopo condizioni sicure.</summary>
    void RequestResetSafety();

    void RequestFaultInjection();

    void RequestAlarmAcknowledge();

    void ApplyDiscretePanel(bool startPulse, bool stopPressed, bool eStopPressed, bool resetSafety, bool faultInject, bool alarmAck);

    MachineSimulationOptions Options { get; }
}
