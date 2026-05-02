namespace ModbusAutomationSimulator.Core.Domain.Enumerations;

/// <summary>
/// Stati principali del ciclo automatico della cella (Idle → caricamento → lavorazione → esito QC → Idle).
/// </summary>
public enum MachineState : ushort
{
    /// <summary>Nessuna lavorazione in corso, attesa comandi.</summary>
    Idle = 0,

    /// <summary>Fase trasporto/caricamento pezzo sulla stazione (simulatore: timer + nastro attivo).</summary>
    Loading = 1,

    /// <summary>Lavorazione meccanica / processo sulla stazione.</summary>
    Processing = 2,

    /// <summary>Fase di controllo qualità (isolata per permettere sensori QC dedicati).</summary>
    QualityInspect = 3,

    /// <summary>Pezzo accettato, scarico verso magazzino uscita.</summary>
    Completed = 4,

    /// <summary>Pezzo respinto dopo QC.</summary>
    Rejected = 5,

    /// <summary>Allarmi non resettabili senza comando operatore dopo fault.</summary>
    Faulted = 6,

    /// <summary>Emergency stop latched: ciclo fermato.</summary>
    EmergencyStopActive = 7
}
