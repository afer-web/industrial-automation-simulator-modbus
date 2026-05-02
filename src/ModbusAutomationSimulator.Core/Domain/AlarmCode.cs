namespace ModbusAutomationSimulator.Core.Domain;

/// <summary>
/// Codici allarme/fault esposti anche su holding register per integrazione SCADA.</summary>
public enum AlarmCode : ushort
{
    None = 0,

    /// <summary>Emergency stop attivato da pannello o da Modbus.</summary>
    EmergencyStop = 1001,

    /// <summary>Fault simulato (fault injection).</summary>
    SimulatedFault = 2001,

    /// <summary>Sovratemperatura stazione (simulato).</summary>
    StationOvertemperature = 2002,

    /// <summary>Pressione idraulica bassa (simulato).</summary>
    LowHydraulicPressure = 2003,

    /// <summary>Timeout trasporto pezzo.</summary>
    ConveyorTimeout = 3001,

    /// <summary>Errore sensore presenza stazione.</summary>
    StationSensorDrift = 3002
}
