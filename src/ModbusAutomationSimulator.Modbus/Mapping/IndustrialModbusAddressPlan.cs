namespace ModbusAutomationSimulator.Modbus.Mapping;

/// <summary>Mappatura registri/indirizzi coils per integrazione OPC/PLC esterna.</summary>
/// <remarks>
/// Piano per test con Modbus Poll e simili: <c>docs/ModbusRegistersModbusPoll.md</c>.
/// <para>
/// <b>COIL USCITE (simulate attuatori, FC01 lettura slave)</b>:
/// Bit 0‑7 segnali fisici comando campo.
/// </para>
/// <para>
/// <b>DISCRETE INPUT FC02</b>: sensori (scritti allo slave dall'asset simulato).
/// </para>
/// <para>
/// <b>INPUT REGISTER FC04</b>: ingressi analogici read-only derivati dai sensori simulati.</para>
/// <para>
/// <b>HOLDING REGISTER FC03/FC16</b>: stati ciclo, codici di allarme, contatori 32‑bit LE.</para>
/// <para>
/// <b>REGIONE COMANDO COIL FC05</b>: comandata da supervisory / SCADA, consumata dall'RTS simulatore.
/// Le bobine di comando vengono azzerate dall'RTS dopo il fronte per handshake pulse.</para>
/// </remarks>
internal static class IndustrialModbusAddressPlan
{
    // --- Bobine USCITE ---
    internal const ushort OutConveyorMotor = 0;
    internal const ushort OutStationHydraulic = 1;
    internal const ushort OutRejectDiverter = 2;
    internal const ushort OutStackTowerGreen = 3;
    internal const ushort OutStackTowerAmber = 4;
    internal const ushort OutStackTowerRed = 5;

    // --- Ingressi supervisor / HMI ---
    internal const ushort CmdStartAutomatic = 96;
    internal const ushort CmdStopSoft = 97;
    internal const ushort CmdEmergencyStopLatch = 98;
    internal const ushort CmdResetSafetyPulse = 99;
    internal const ushort CmdFaultInjectPulse = 100;
    internal const ushort CmdAlarmAckPulse = 101;

    // --- Discrete Inputs (ingressi PLC) ---
    internal const ushort DiConveyorMotorFeedback = 0;
    internal const ushort DiPieceAtEntry = 1;
    internal const ushort DiPieceAtStation = 2;
    internal const ushort DiClampEngagedSensor = 3;
    internal const ushort DiQualityMeasuringLane = 4;
    internal const ushort DiOutboundLaneReady = 5;

    // --- Input Registers (analog ingressi PLC) ---
    internal const ushort IrConveyorSpeedPermille = 0; // scala 0..1000 ⇒ 100% ⇒ 1000
    internal const ushort IrStationTemperatureScaled = 1; // /10 °C sintetizzato
    internal const ushort IrHydraulicBarScaled = 2;       // /10 bar sintetizzato

    // --- Holding Registers ---
    internal const ushort HrMachineStateWord = 0;
    internal const ushort HrStatusBitmask = 1;
    internal const ushort HrGoodPiecesLo = 2;
    internal const ushort HrGoodPiecesHi = 3;
    internal const ushort HrScrapPiecesLo = 4;
    internal const ushort HrScrapPiecesHi = 5;
    internal const ushort HrPhaseElapsedMs = 6;
    internal const ushort HrTotalCycleElapsedMs = 7;
    internal const ushort HrAlarmCodeRaw = 8;
}
