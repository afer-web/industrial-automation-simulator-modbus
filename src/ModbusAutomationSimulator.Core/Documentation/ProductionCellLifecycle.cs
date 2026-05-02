namespace ModbusAutomationSimulator.Core.Documentation;

/// <summary>
/// <para>
/// State machine ciclica implementata da <see cref="Simulation.ProductionCellOrchestrator"/>.
/// </para>
/// <list type="bullet">
/// <item>
/// Idle + richiesta ciclo (<c>_autoRunDesired=true</c>, nessuna condizione sicurezza bloccante) → Loading
/// carico simulazione con nastro dedicato alla fase caricamento timer <see cref="Domain.MachineSimulationOptions.LoadingDurationMs"/>.
/// </item>
/// <item>Loading terminata → Processing (clamp chiusura simulato verso fine fase caricamento).</item>
/// <item>Processing → QualityInspect dopo <see cref="Domain.MachineSimulationOptions.ProcessingDurationMs"/> ms.</item>
/// <item>QualityInspect deterministico probabilisticamente → Completed (OK) incrementa Good) oppure Rejected (SCRAP increment).</item>
/// <item>
/// Completed / Rejected dopo timer scarico/esclusione tornano Idle oppure caricano ciclo continuativo automatico (<c>_haltAfterCycle</c> blocca cicli successivi).</item>
/// <item>Emergency stop forzato (UI/Modbus) → EmergencyStopActive, allarme <see cref="Domain.AlarmCode.EmergencyStop"/>.</item>
/// <item>Fault injection (UI/Modbus) → stato Fault + allarme random di guasto simulato.</item>
/// </list>
/// </summary>
internal static partial class ProductionCellLifecycle
{
}
