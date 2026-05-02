using System.Runtime.InteropServices;
using FluentModbus;
using ModbusAutomationSimulator.Core.Contracts;
using ModbusAutomationSimulator.Core.Domain.Enumerations;

namespace ModbusAutomationSimulator.Modbus.Mapping;

internal static class IndustrialModbusMapper
{
    internal const byte DefaultUnitIdentifier = 0;

    internal static void ProcessSupervisoryCoils(ModbusTcpServer server,
        IProductionCellOrchestrator orchestrator,
        IndustrialSupervisorLatch latch)
    {
        Span<byte> coilBits = AcquireCoils(server);

        bool rawStart = coilBits.Get(IndustrialModbusAddressPlan.CmdStartAutomatic);
        bool rawFault = coilBits.Get(IndustrialModbusAddressPlan.CmdFaultInjectPulse);
        bool rawAck = coilBits.Get(IndustrialModbusAddressPlan.CmdAlarmAckPulse);
        bool rawReset = coilBits.Get(IndustrialModbusAddressPlan.CmdResetSafetyPulse);

        bool startPulse = rawStart && !latch.PrevStartRaw;
        bool faultPulse = rawFault && !latch.PrevFaultRaw;
        bool ackPulse = rawAck && !latch.PrevAckRaw;
        bool resetPulse = rawReset && !latch.PrevResetRaw;

        bool stopHeld = coilBits.Get(IndustrialModbusAddressPlan.CmdStopSoft);
        bool eStopHeld = coilBits.Get(IndustrialModbusAddressPlan.CmdEmergencyStopLatch);

        latch.PrevStartRaw = rawStart;
        latch.PrevEmergencyRaw = eStopHeld;
        latch.PrevFaultRaw = rawFault;
        latch.PrevAckRaw = rawAck;
        latch.PrevResetRaw = rawReset;

        orchestrator.ApplyDiscretePanel(
            startPulse,
            stopPressed: stopHeld,
            eStopPressed: eStopHeld,
            resetSafety: resetPulse,
            faultInject: faultPulse,
            alarmAck: ackPulse);

        coilBits.Set(IndustrialModbusAddressPlan.CmdFaultInjectPulse, false);
        coilBits.Set(IndustrialModbusAddressPlan.CmdAlarmAckPulse, false);
        coilBits.Set(IndustrialModbusAddressPlan.CmdResetSafetyPulse, false);
        coilBits.Set(IndustrialModbusAddressPlan.CmdStartAutomatic, false);
    }

    internal static void ApplyMachineSnapshot(ModbusTcpServer server, MachineSnapshot snapshot)
    {
        Span<byte> coils = AcquireCoils(server);

        coils.Set(IndustrialModbusAddressPlan.OutConveyorMotor,
            snapshot.DiscreteSensors.ConveyorMotorRunning);

        coils.Set(IndustrialModbusAddressPlan.OutStationHydraulic,
            snapshot.DiscreteSensors.StationClampEngaged);

        coils.Set(IndustrialModbusAddressPlan.OutRejectDiverter,
            snapshot.MachineState == MachineState.Rejected);

        bool healthyIdle =
            snapshot.MachineState == MachineState.Idle &&
            snapshot.ActiveAlarmRegister == 0;

        bool cycleBusy =
            snapshot.IsRunningAutomatic ||
            snapshot.MachineState is MachineState.Loading
                or MachineState.Processing
                or MachineState.QualityInspect
                or MachineState.Completed;

        coils.Set(IndustrialModbusAddressPlan.OutStackTowerGreen, healthyIdle);
        coils.Set(IndustrialModbusAddressPlan.OutStackTowerAmber,
            cycleBusy && snapshot.ActiveAlarmRegister == 0);

        bool fatalVisual =
            snapshot.ActiveAlarmRegister != 0 ||
            ((snapshot.StatusWordBits >> 9) & 1) != 0;

        coils.Set(IndustrialModbusAddressPlan.OutStackTowerRed, fatalVisual);

        // Discrete inputs (= sensor field)
        Span<byte> discrete = AcquireDiscreteInputs(server);

        discrete.Set(IndustrialModbusAddressPlan.DiConveyorMotorFeedback,
            snapshot.DiscreteSensors.ConveyorMotorRunning);

        discrete.Set(IndustrialModbusAddressPlan.DiPieceAtEntry,
            snapshot.DiscreteSensors.PieceAtConveyorEntry);

        discrete.Set(IndustrialModbusAddressPlan.DiPieceAtStation,
            snapshot.DiscreteSensors.PieceAtStation);

        discrete.Set(IndustrialModbusAddressPlan.DiClampEngagedSensor,
            snapshot.DiscreteSensors.StationClampEngaged);

        discrete.Set(IndustrialModbusAddressPlan.DiQualityMeasuringLane,
            snapshot.DiscreteSensors.QualityStationActive);

        discrete.Set(IndustrialModbusAddressPlan.DiOutboundLaneReady,
            snapshot.DiscreteSensors.WarehouseOutboundReady);

        // Input registers analog
        Span<short> inputRegs = AcquireInputRegisters(server);

        ushort speedPermille =
            Math.Min(snapshot.AnalogSensors.ConveyorSpeedPercent, (ushort)1000);

        inputRegs.SetLittleEndian(IndustrialModbusAddressPlan.IrConveyorSpeedPermille, speedPermille);

        ushort tempScaled =
            unchecked((ushort)Math.Min((uint)snapshot.AnalogSensors.StationTemperatureC * 10,
                ushort.MaxValue));

        inputRegs.SetLittleEndian(IndustrialModbusAddressPlan.IrStationTemperatureScaled, tempScaled);

        ushort hydScaled =
            unchecked((ushort)Math.Min((uint)(snapshot.AnalogSensors.HydraulicPressureBar / 10),
                ushort.MaxValue));

        inputRegs.SetLittleEndian(IndustrialModbusAddressPlan.IrHydraulicBarScaled, hydScaled);

        // Holding registers di stato/contatori
        Span<short> holding = AcquireHoldingRegisters(server);

        holding.SetLittleEndian(IndustrialModbusAddressPlan.HrMachineStateWord,
            (ushort)snapshot.MachineState);

        holding.SetLittleEndian(IndustrialModbusAddressPlan.HrStatusBitmask, snapshot.StatusWordBits);

        unchecked
        {
            uint goods = (uint)Math.Max(0, snapshot.GoodPiecesCount);
            uint scrap = (uint)Math.Max(0, snapshot.ScrapPiecesCount);

            holding.SetLittleEndian(IndustrialModbusAddressPlan.HrGoodPiecesLo, (ushort)goods);
            holding.SetLittleEndian(IndustrialModbusAddressPlan.HrGoodPiecesHi, (ushort)(goods >> 16));

            holding.SetLittleEndian(IndustrialModbusAddressPlan.HrScrapPiecesLo, (ushort)scrap);
            holding.SetLittleEndian(IndustrialModbusAddressPlan.HrScrapPiecesHi, (ushort)(scrap >> 16));
        }

        holding.SetLittleEndian(IndustrialModbusAddressPlan.HrPhaseElapsedMs, snapshot.PhaseElapsedMs);

        holding.SetLittleEndian(IndustrialModbusAddressPlan.HrTotalCycleElapsedMs,
            snapshot.TotalCycleElapsedMs);

        holding.SetLittleEndian(IndustrialModbusAddressPlan.HrAlarmCodeRaw, snapshot.ActiveAlarmRegister);
    }

    static Span<byte> AcquireCoils(ModbusTcpServer server) =>
        server.GetCoilBuffer(DefaultUnitIdentifier);

    static Span<byte> AcquireDiscreteInputs(ModbusTcpServer server) =>
        server.GetDiscreteInputBuffer(DefaultUnitIdentifier);

    static Span<short> AcquireInputRegisters(ModbusTcpServer server)
    {
        Span<byte> bytes = server.GetInputRegisterBuffer(DefaultUnitIdentifier);

        int even = bytes.Length & ~1;

        return MemoryMarshal.Cast<byte, short>(bytes[..even]);
    }

    static Span<short> AcquireHoldingRegisters(ModbusTcpServer server)
    {
        Span<byte> bytes = server.GetHoldingRegisterBuffer(DefaultUnitIdentifier);
        int even = bytes.Length & ~1;

        return MemoryMarshal.Cast<byte, short>(bytes[..even]);
    }
}
