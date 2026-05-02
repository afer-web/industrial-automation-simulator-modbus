namespace ModbusAutomationSimulator.Modbus.Configuration;

/// <summary>Ossatura di ascolto TCP e periodo di aggiornamento mappa I/O.</summary>
public sealed class IndustrialModbusOptions
{
    /// <summary>Indirizzo IPv4 o <c>+</c> / <c>*</c> per <see cref="System.Net.IPAddress.Any"/>.</summary>
    public string ListenAddress { get; set; } = "+";

    /// <summary>Porta TCP slave (su Windows spesso diverso da 502 per permessi amministrativi).</summary>
    public int Port { get; set; } = 502;

    /// <summary>Periodo ciclo PLC simulato (ms).</summary>
    public int ScanMilliseconds { get; set; } = 33;

    /// <summary>Abilita log diagnostici scan Modbus brevi.</summary>
    public bool VerboseTracing { get; set; }
}
