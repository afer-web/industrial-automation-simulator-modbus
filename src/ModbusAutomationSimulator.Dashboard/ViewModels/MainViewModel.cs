using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModbusAutomationSimulator.Core.Contracts;

namespace ModbusAutomationSimulator.Dashboard.ViewModels;

/// <summary>ViewModel principale: comando macchina locale e stato sinottico (Modbus viene servito dall'RTS in background).</summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    readonly IProductionCellOrchestrator _orchestrator;
    readonly DispatcherTimer _refreshTimer;

    [ObservableProperty] private string _machinePhase = "Idle";

    [ObservableProperty] private string _alarmActive = "—";

    [ObservableProperty] private int _goodCount;

    [ObservableProperty] private int _scrapCount;

    [ObservableProperty] private ushort _phaseMs;

    [ObservableProperty] private string _statusWordHex = "0x0000";

    [ObservableProperty] private bool _lampGreen;

    [ObservableProperty] private bool _lampAmber;

    [ObservableProperty] private bool _lampRed;

    [ObservableProperty] private bool _sensorConveyor;

    [ObservableProperty] private bool _sensorEntryPiece;

    [ObservableProperty] private bool _sensorStationPiece;

    [ObservableProperty] private bool _sensorQualityLane;

    [ObservableProperty] private bool _sensorOutboundReady;

    public MainViewModel(IProductionCellOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        orchestrator.TelemetryChanged += OnTelemetryChanged;

        _refreshTimer =
            new DispatcherTimer(DispatcherPriority.Background, System.Windows.Application.Current!.Dispatcher!)
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };

        _refreshTimer.Tick += (_, _) => RefreshFromSnapshot(_orchestrator.Snapshot);
        _refreshTimer.Start();
        RefreshFromSnapshot(_orchestrator.Snapshot);
    }

    void OnTelemetryChanged(object? _, MachineTelemetryChangedEventArgs e) =>
        System.Windows.Application.Current!.Dispatcher.BeginInvoke(
            () => RefreshFromSnapshot(e.Snapshot),
            DispatcherPriority.DataBind);

    void RefreshFromSnapshot(MachineSnapshot s)
    {
        MachinePhase = s.MachineState.ToString();
        AlarmActive = s.ActiveAlarmRegister == 0 ? "—" : $"{s.ActiveAlarmRegister}";
        GoodCount = s.GoodPiecesCount;
        ScrapCount = s.ScrapPiecesCount;
        PhaseMs = s.PhaseElapsedMs;
        StatusWordHex = $"0x{s.StatusWordBits:X4}";
        ushort st = s.StatusWordBits;

        LampGreen = (st & (1 << 6)) != 0 || (s.MachineState == Core.Domain.Enumerations.MachineState.Idle &&
                                           s.ActiveAlarmRegister == 0);
        LampAmber = (st & (1 << 0)) != 0 || (st & (1 << 8)) != 0;
        LampRed = s.ActiveAlarmRegister != 0 || (st & (1 << 9)) != 0;

        SensorConveyor = s.DiscreteSensors.ConveyorMotorRunning;
        SensorEntryPiece = s.DiscreteSensors.PieceAtConveyorEntry;
        SensorStationPiece = s.DiscreteSensors.PieceAtStation;
        SensorQualityLane = s.DiscreteSensors.QualityStationActive;
        SensorOutboundReady = s.DiscreteSensors.WarehouseOutboundReady;
    }

    [RelayCommand]
    void StartCycle() => _orchestrator.RequestStartAutomatic();

    [RelayCommand]
    void SoftStop() => _orchestrator.RequestStopAutomatic();

    [RelayCommand]
    void EmergencyStop() => _orchestrator.RequestEmergencyStop();

    [RelayCommand]
    void ResetSafety() => _orchestrator.RequestResetSafety();

    [RelayCommand]
    void FaultInject() => _orchestrator.RequestFaultInjection();

    [RelayCommand]
    void AckAlarms() => _orchestrator.RequestAlarmAcknowledge();

    /// <inheritdoc />
    public void Dispose()
    {
        _refreshTimer.Stop();
        _orchestrator.TelemetryChanged -= OnTelemetryChanged;
        GC.SuppressFinalize(this);
    }
}
