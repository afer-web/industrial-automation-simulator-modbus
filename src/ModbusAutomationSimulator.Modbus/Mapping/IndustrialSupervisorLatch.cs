namespace ModbusAutomationSimulator.Modbus.Mapping;

/// <summary>Memorie booleane del ciclo precedente per estrarre fronti da bobine comandate via Modbus FC05.</summary>
internal sealed class IndustrialSupervisorLatch
{
    internal bool PrevStartRaw;
    internal bool PrevEmergencyRaw;
    internal bool PrevFaultRaw;
    internal bool PrevAckRaw;
    internal bool PrevResetRaw;
}
