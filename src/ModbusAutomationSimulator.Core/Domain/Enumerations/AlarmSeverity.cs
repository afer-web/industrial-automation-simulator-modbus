namespace ModbusAutomationSimulator.Core.Domain.Enumerations;

/// <summary>
/// Livello di severità degli allarmi (per UI e priorità).</summary>
public enum AlarmSeverity : byte
{
    Info = 0,
    Warning = 1,
    Fault = 2
}
