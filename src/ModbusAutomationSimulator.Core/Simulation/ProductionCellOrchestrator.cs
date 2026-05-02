using System.Globalization;
using ModbusAutomationSimulator.Core.Contracts;
using ModbusAutomationSimulator.Core.Domain;
using ModbusAutomationSimulator.Core.Domain.Enumerations;

namespace ModbusAutomationSimulator.Core.Simulation;

/// <summary>
/// Orchestrazione cella: ciclo Idle → caricamento → lavorazione → QC → Completed/Rejected → Idle,
/// allarmi, fault injection, sensori sintetici.</summary>
public sealed class ProductionCellOrchestrator : IProductionCellOrchestrator
{
    readonly object _gate = new();
    readonly Random _rng = Random.Shared;
    readonly List<AlarmRecord> _alarmBuffer = new();

    MachineState _machineState = MachineState.Idle;
    bool _autoRunDesired;
    bool _haltAfterCycle;
    bool _eStopLatched;

    ushort _phaseElapsedMs;
    ushort _cycleElapsedMs;

    ushort _unloadPhaseMs;
    ushort _ambientSeed;

    int _goodPieces;
    int _scrapPieces;

    bool _prevStop;
    bool _prevEstop;
    bool _prevFaultInject;
    bool _prevAlarmAck;
    bool _prevResetSafety;

    public ProductionCellOrchestrator(MachineSimulationOptions options) => Options = options;

    /// <inheritdoc />
    public MachineSimulationOptions Options { get; }

    /// <inheritdoc />
    public MachineSnapshot Snapshot
    {
        get
        {
            lock (_gate)
                return CaptureSnapshotUnsafe();
        }
    }

    /// <inheritdoc />
    public event EventHandler<MachineTelemetryChangedEventArgs>? TelemetryChanged;

    /// <inheritdoc />
    public void RequestStartAutomatic()
    {
        lock (_gate)
            BeginAutoUnsafe();

        RaiseTelemetryOutsideGate();
    }

    /// <inheritdoc />
    public void RequestStopAutomatic()
    {
        lock (_gate)
            StopUnsafe();

        RaiseTelemetryOutsideGate();
    }

    /// <inheritdoc />
    public void RequestEmergencyStop()
    {
        lock (_gate)
            EnterEmergencyUnsafe("Emergency stop commanded from dashboard.");

        RaiseTelemetryOutsideGate();
    }

    /// <inheritdoc />
    public void RequestResetSafety()
    {
        lock (_gate)
            ResetSafetyUnsafe();

        RaiseTelemetryOutsideGate();
    }

    /// <inheritdoc />
    public void RequestFaultInjection()
    {
        lock (_gate)
            TriggerFaultInjectionUnsafe(force: false);

        RaiseTelemetryOutsideGate();
    }

    /// <inheritdoc />
    public void RequestAlarmAcknowledge()
    {
        lock (_gate)
            AcknowledgeAlarmsUnsafe(a => a.AcknowledgedAtUtc is null);

        RaiseTelemetryOutsideGate();
    }

    /// <inheritdoc />
    public void ApplyDiscretePanel(
        bool startPulse,
        bool stopPressed,
        bool eStopPressed,
        bool resetSafety,
        bool faultInject,
        bool alarmAck)
    {
        lock (_gate)
        {
            bool eStopPulse = eStopPressed && !_prevEstop;
            bool resetPulse = resetSafety && !_prevResetSafety;
            bool faultPulse = faultInject && !_prevFaultInject;
            bool ackPulse = alarmAck && !_prevAlarmAck;
            bool stopPulse = stopPressed && !_prevStop;

            if (startPulse)
                BeginAutoUnsafe();

            if (resetPulse)
                ResetSafetyUnsafe();

            if (eStopPulse)
                EnterEmergencyUnsafe("Emergency stop from operator panel.");

            if (stopPulse)
                StopUnsafe();

            if (ackPulse)
                AcknowledgeAlarmsUnsafe(a => a.AcknowledgedAtUtc is null);

            if (faultPulse)
                TriggerFaultInjectionUnsafe(force: true);

            _prevStop = stopPressed;
            _prevEstop = eStopPressed;
            _prevFaultInject = faultInject;
            _prevAlarmAck = alarmAck;
            _prevResetSafety = resetSafety;
        }

        RaiseTelemetryOutsideGate();
    }

    /// <inheritdoc />
    public void Tick(TimeSpan delta)
    {
        lock (_gate)
        {
            int scaledMs = (int)Math.Clamp(delta.TotalMilliseconds * Options.TimeScale, 0, 250);
            _ambientSeed = unchecked((ushort)((_ambientSeed + 17) ^ (scaledMs ^ (_rng.Next() & 0xFF))));

            if (scaledMs > 0)
            {
                if (!_eStopLatched && _machineState != MachineState.EmergencyStopActive)
                    TickAutomationUnsafe(scaledMs);

                _cycleElapsedMs = AddClampedMs(_cycleElapsedMs, scaledMs);
            }
        }

        RaiseTelemetryOutsideGate();
    }

    /// <summary>
    /// Arresto ordinato «fine ciclo»: non avvia nuovi cicli dopo lo scarico del pezzo attuale;
    /// caricamento/lavorazione/QC/scarico in corso continuano fino allo stato Idle (durata da <see cref="MachineSimulationOptions"/>).
    /// </summary>
    void StopUnsafe()
    {
        _haltAfterCycle = true;
        _autoRunDesired = false;
    }

    void ResetSafetyUnsafe()
    {
        if (!_eStopLatched && _machineState is not MachineState.Faulted and not MachineState.EmergencyStopActive)
            return;

        ClearFaultAlarmsUnsafe();
        _eStopLatched = false;
        _haltAfterCycle = false;
        _machineState = MachineState.Idle;
        _phaseElapsedMs = 0;
        _cycleElapsedMs = 0;
    }

    void BeginAutoUnsafe()
    {
        if (_machineState is MachineState.Faulted or MachineState.EmergencyStopActive || _eStopLatched)
            return;

        _autoRunDesired = true;
        _haltAfterCycle = false;

        if (_machineState == MachineState.Idle && HasBlockingFaultUnsafe())
            return;

        TransitionFromIdleUnsafe();
    }

    bool HasBlockingFaultUnsafe() =>
        _alarmBuffer.Any(a =>
            a.AcknowledgedAtUtc is null &&
            a.Code != AlarmCode.None &&
            (a.Code == AlarmCode.EmergencyStop || a.Severity == AlarmSeverity.Fault));

    void TransitionFromIdleUnsafe()
    {
        if (!_autoRunDesired || _haltAfterCycle)
            return;

        _machineState = MachineState.Loading;
        _phaseElapsedMs = 0;
        _cycleElapsedMs = 0;
    }

    void TickAutomationUnsafe(int scaledMs)
    {
        switch (_machineState)
        {
            case MachineState.Idle:
                TransitionFromIdleUnsafe();
                break;

            case MachineState.Loading:
                AdvancePhase(ref _phaseElapsedMs, Options.LoadingDurationMs, scaledMs, () =>
                {
                    _machineState = MachineState.Processing;
                    _phaseElapsedMs = 0;
                });
                break;

            case MachineState.Processing:
                AdvancePhase(ref _phaseElapsedMs, Options.ProcessingDurationMs, scaledMs, () =>
                {
                    _machineState = MachineState.QualityInspect;
                    _phaseElapsedMs = 0;
                });
                break;

            case MachineState.QualityInspect:
                AdvancePhase(ref _phaseElapsedMs, Options.QualityInspectDurationMs, scaledMs, () =>
                {
                    bool ok = _rng.NextDouble() < Options.QualityAcceptProbability;
                    _machineState = ok ? MachineState.Completed : MachineState.Rejected;
                    _unloadPhaseMs = 0;

                    if (ok)
                        _goodPieces++;
                    else
                        _scrapPieces++;
                });
                break;

            case MachineState.Completed:
                AdvanceUnload(ref _unloadPhaseMs, Options.CompletedUnloadMs, scaledMs,
                    GotoIdleBetweenCyclesUnsafe);
                break;

            case MachineState.Rejected:
                AdvanceUnload(ref _unloadPhaseMs, Options.RejectedEjectMs, scaledMs,
                    GotoIdleBetweenCyclesUnsafe);
                break;

            case MachineState.Faulted:
            case MachineState.EmergencyStopActive:
                break;
        }
    }

    void GotoIdleBetweenCyclesUnsafe()
    {
        if (_haltAfterCycle || !_autoRunDesired)
            _machineState = MachineState.Idle;
        else
            TransitionFromIdleUnsafe();

        _phaseElapsedMs = 0;
        _unloadPhaseMs = 0;
    }

    static void AdvancePhase(ref ushort accumulator, int limitMs, int delta, Action wrap)
    {
        int sum = accumulator + delta;
        if (sum >= limitMs)
        {
            wrap();
            return;
        }

        accumulator = (ushort)Math.Min(sum, ushort.MaxValue);
    }

    static void AdvanceUnload(ref ushort acc, int limitMs, int delta, Action done)
    {
        int sum = acc + delta;
        if (sum >= limitMs)
            done();
        else
            acc = (ushort)Math.Min(sum, ushort.MaxValue);
    }

    ushort AddClampedMs(ushort cur, int add) =>
        cur >= ushort.MaxValue - add ? ushort.MaxValue : (ushort)(cur + add);

    void EnterEmergencyUnsafe(string message)
    {
        _eStopLatched = true;
        _autoRunDesired = false;
        _machineState = MachineState.EmergencyStopActive;
        RaiseAlarm(AlarmCode.EmergencyStop, AlarmSeverity.Fault, message);
    }

    void TriggerFaultInjectionUnsafe(bool force)
    {
        if (_machineState is MachineState.Faulted or MachineState.EmergencyStopActive || _eStopLatched)
            return;

        AlarmCode faultCode = force ? AlarmCode.SimulatedFault : PickRandomFault();
        _machineState = MachineState.Faulted;
        _autoRunDesired = false;

        RaiseAlarm(
            faultCode,
            AlarmSeverity.Fault,
            string.Create(CultureInfo.InvariantCulture,
                $"{faultCode:G} injected at {_cycleElapsedMs} ms cycle elapsed."));
    }

    AlarmCode PickRandomFault()
    {
        AlarmCode[] pool =
        [
            AlarmCode.SimulatedFault,
            AlarmCode.StationOvertemperature,
            AlarmCode.LowHydraulicPressure,
            AlarmCode.ConveyorTimeout,
            AlarmCode.StationSensorDrift
        ];

        return pool[_rng.Next(pool.Length)];
    }

    void ClearFaultAlarmsUnsafe()
    {
        _alarmBuffer.RemoveAll(a =>
            a.Code is AlarmCode.EmergencyStop ||
            (a.AcknowledgedAtUtc is null && a.Severity == AlarmSeverity.Fault));
    }

    void AcknowledgeAlarmsUnsafe(Func<AlarmRecord, bool> predicate)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        for (int i = 0; i < _alarmBuffer.Count; i++)
        {
            AlarmRecord alarm = _alarmBuffer[i];

            if (alarm.AcknowledgedAtUtc is null && predicate(alarm))
                _alarmBuffer[i] = alarm with { AcknowledgedAtUtc = now };
        }
    }

    void RaiseAlarm(AlarmCode code, AlarmSeverity severity, string message)
    {
        if (code == AlarmCode.None || string.IsNullOrWhiteSpace(message))
            return;

        _alarmBuffer.Add(new AlarmRecord(
            Guid.NewGuid(),
            code,
            message,
            severity,
            DateTimeOffset.UtcNow,
            AcknowledgedAtUtc: null));

        TrimAlarmsUnsafe();
    }

    void TrimAlarmsUnsafe()
    {
        const int max = 100;
        if (_alarmBuffer.Count <= max)
            return;

        _alarmBuffer.RemoveRange(0, _alarmBuffer.Count - max);
    }

    MachineSnapshot CaptureSnapshotUnsafe()
    {
        DiscreteSensorState discrete = BuildDiscrete(
            Options,
            _machineState,
            _phaseElapsedMs,
            _unloadPhaseMs,
            _autoRunDesired);

        AnalogSensorState analog = BuildAnalog(discrete);

        AlarmRecord? headline = PickHeadlineAlarmUnsafe();
        ushort activeCode = headline is null ? (ushort)0 : (ushort)headline.Code;

        ushort bits = ComposeStatusBitsUnsafe(discrete, headline is not null);

        return new MachineSnapshot(
            _machineState,
            activeCode,
            discrete,
            analog,
            _goodPieces,
            _scrapPieces,
            PhaseProgressDisplayUnsafe(),
            _cycleElapsedMs,
            IsAutomaticCycleUnsafe(),
            false,
            bits);
    }

    bool IsAutomaticCycleUnsafe()
    {
        bool inAutomaticPhases =
            _machineState is MachineState.Loading
                or MachineState.Processing
                or MachineState.QualityInspect
                or MachineState.Completed
                or MachineState.Rejected;

        bool safetyOk =
            !_eStopLatched
            && _machineState is not MachineState.Faulted
                and not MachineState.EmergencyStopActive;

        return _autoRunDesired && inAutomaticPhases && safetyOk;
    }

    ushort PhaseProgressDisplayUnsafe() =>
        _machineState switch
        {
            MachineState.Completed => _unloadPhaseMs,
            MachineState.Rejected => _unloadPhaseMs,
            _ => _phaseElapsedMs,
        };

    AlarmRecord? PickHeadlineAlarmUnsafe() =>
        _alarmBuffer
            .Where(a => a.Code != AlarmCode.None && a.AcknowledgedAtUtc is null)
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.RaisedAtUtc)
            .FirstOrDefault();

    ushort ComposeStatusBitsUnsafe(DiscreteSensorState d, bool alarmLatch)
    {
        ushort w = 0;

        SetBit(ref w, 0, IsAutomaticCycleUnsafe());

        SetBit(ref w, 1, alarmLatch || _alarmBuffer.Exists(a =>
            a.AcknowledgedAtUtc is null &&
            (a.Severity >= AlarmSeverity.Warning || a.Code == AlarmCode.EmergencyStop)));

        SetBit(ref w, 2, _machineState == MachineState.Faulted ||
                      _alarmBuffer.Exists(a =>
                          a.AcknowledgedAtUtc is null && a.Code == AlarmCode.SimulatedFault));

        SetBit(ref w, 3, d.ConveyorMotorRunning);
        SetBit(ref w, 4, d.StationClampEngaged);
        SetBit(ref w, 5, d.QualityStationActive);

        SetBit(ref w, 6, _machineState == MachineState.Completed);
        SetBit(ref w, 7, _machineState == MachineState.Rejected);
        SetBit(ref w, 8, _haltAfterCycle);
        SetBit(ref w, 9, _eStopLatched || _machineState == MachineState.EmergencyStopActive);

        return w;
    }

    static void SetBit(ref ushort word, int bit, bool flag)
    {
        if (flag)
            word |= (ushort)(1 << bit);
    }

    AnalogSensorState BuildAnalog(DiscreteSensorState d)
    {
        ushort speed = d.ConveyorMotorRunning ? (ushort)850 : (ushort)0;

        double wave = Math.Sin((_ambientSeed + _cycleElapsedMs) * Math.PI / 180.0) * 3.7;
        ushort temp =
            (ushort)Math.Clamp(205 + wave + (_machineState == MachineState.Processing ? 8 : 0), 185, 255);

        double pressureNoise = ((_ambientSeed ^ _cycleElapsedMs) & 0x1FF) / 100.0;
        ushort hydraulic =
            (ushort)Math.Clamp(1020 + pressureNoise + (_machineState == MachineState.Faulted ? -120 : 0), 150,
                short.MaxValue);

        return new AnalogSensorState(speed, temp, hydraulic);
    }

    /// <summary>
    /// Notifica telemetry dopo aver ricalcolato lo stato sotto lock e poi rilasciato <c>_gate</c>,
    /// così i listener UI (dispatcher sincrono su thread RTS) non restano bloccati.
    /// </summary>
    void RaiseTelemetryOutsideGate()
    {
        MachineSnapshot snapshot;
        lock (_gate)
            snapshot = CaptureSnapshotUnsafe();

        TelemetryChanged?.Invoke(this, new MachineTelemetryChangedEventArgs(snapshot));
    }

    static DiscreteSensorState BuildDiscrete(
        MachineSimulationOptions opts,
        MachineState state,
        ushort phaseElapsed,
        ushort unloadElapsed,
        bool autoDesired)
    {
        float loadRatio = opts.LoadingDurationMs <= 0
            ? 1f
            : Math.Clamp(phaseElapsed / (float)Math.Min(opts.LoadingDurationMs, ushort.MaxValue), 0f, 1f);

        float unloadRatioCompleted = UnloadRatio(opts.CompletedUnloadMs, unloadElapsed);
        float unloadRatioReject = UnloadRatio(opts.RejectedEjectMs, unloadElapsed);

        bool conveyor =
            state == MachineState.Loading ||
            (state == MachineState.Completed && unloadRatioCompleted < 0.45f) ||
            (state == MachineState.Rejected && unloadRatioReject < 0.45f);

        bool pieceEntry = state == MachineState.Loading && loadRatio is > 0.05f and < 0.55f;

        bool pieceStation = state switch
        {
            MachineState.Loading => loadRatio >= 0.65f,
            MachineState.Processing or MachineState.QualityInspect => true,
            MachineState.Completed => unloadRatioCompleted < 0.85f,
            MachineState.Rejected => unloadRatioReject < 0.85f,
            _ => false,
        };

        bool clamp = state switch
        {
            MachineState.Processing => true,
            MachineState.Loading => loadRatio >= 0.92f,
            MachineState.QualityInspect => true,
            _ => false,
        };

        bool qcStation = state == MachineState.QualityInspect;

        bool outboundReady =
            (state == MachineState.Completed && unloadRatioCompleted > 0.55f)
            || (state == MachineState.Rejected && unloadRatioReject > 0.55f)
            || (state == MachineState.Idle && !autoDesired);

        return new DiscreteSensorState(conveyor, pieceEntry, pieceStation, clamp, qcStation, outboundReady);
    }

    static float UnloadRatio(int denomMs, ushort acc) =>
        denomMs <= 0 ? 1f : Math.Clamp(acc / (float)Math.Min(denomMs, ushort.MaxValue), 0f, 1f);
}