using ModbusAutomationSimulator.Core.Domain.Enumerations;

namespace ModbusAutomationSimulator.Core.Domain;

/// <summary>
/// Istanza di allarme con timestamp locale per annuncio UI e acknowledgment.</summary>
public sealed record AlarmRecord(
    Guid Id,
    AlarmCode Code,
    string Message,
    AlarmSeverity Severity,
    DateTimeOffset RaisedAtUtc,
    DateTimeOffset? AcknowledgedAtUtc);
