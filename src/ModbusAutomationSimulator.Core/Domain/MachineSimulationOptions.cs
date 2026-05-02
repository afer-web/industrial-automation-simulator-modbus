namespace ModbusAutomationSimulator.Core.Domain;

/// <summary>
/// Parametri di simulazione caricabili da configurazione.</summary>
public sealed class MachineSimulationOptions
{
    /// <summary>Durata fase caricamento trasporto (ms).</summary>
    public int LoadingDurationMs { get; set; } = 2200;

    /// <summary>Durata lavorazione sulla stazione (ms).</summary>
    public int ProcessingDurationMs { get; set; } = 3500;

    /// <summary>Durata fase QC (ms).</summary>
    public int QualityInspectDurationMs { get; set; } = 1200;

    /// <summary>Durata scarico pezzo OK (ms).</summary>
    public int CompletedUnloadMs { get; set; } = 800;

    /// <summary>Durata espulsione scarto (ms).</summary>
    public int RejectedEjectMs { get; set; } = 1000;

    /// <summary>Probabilità [0..1] che il pezzo sia accettato in QC.</summary>
    public double QualityAcceptProbability { get; set; } = 0.92;

    /// <summary>Accelerazione simulatore (moltiplicatore sul delta temporale).</summary>
    public double TimeScale { get; set; } = 1.0;
}
