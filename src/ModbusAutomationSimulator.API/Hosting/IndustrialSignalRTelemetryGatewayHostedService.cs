using Microsoft.AspNetCore.SignalR;
using ModbusAutomationSimulator.API.Dtos;
using ModbusAutomationSimulator.API.Hubs;
using ModbusAutomationSimulator.Core.Contracts;

namespace ModbusAutomationSimulator.API.Hosting;

/// <summary>Propaga <see cref="IProductionCellOrchestrator.TelemetryChanged"/> su SignalR (<see cref="IndustrialSignalRTelemetryTopics.Telemetry"/>).</summary>
public sealed class IndustrialSignalRTelemetryGatewayHostedService : IHostedService
{
    readonly IHubContext<IndustrialProductionTelemetryHub> _hubContext;
    readonly IProductionCellOrchestrator _orchestrator;
    readonly ILogger<IndustrialSignalRTelemetryGatewayHostedService> _logger;

    public IndustrialSignalRTelemetryGatewayHostedService(
        IHubContext<IndustrialProductionTelemetryHub> hubContext,
        IProductionCellOrchestrator orchestrator,
        ILogger<IndustrialSignalRTelemetryGatewayHostedService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _orchestrator.TelemetryChanged += ProductionOrchestratorOnTelemetryChanged;
        _logger.LogInformation("SignalR telemetry gateway attached to orchestrator.");
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _orchestrator.TelemetryChanged -= ProductionOrchestratorOnTelemetryChanged;
        return Task.CompletedTask;
    }

    void ProductionOrchestratorOnTelemetryChanged(object? _, MachineTelemetryChangedEventArgs eventArgs)
    {
        ProductionTelemetryDto dto = ProductionTelemetryMapper.From(eventArgs.Snapshot);

        _ = Task.Run(async () =>
        {
            try
            {
                await _hubContext.Clients.All
                    .SendAsync(IndustrialSignalRTelemetryTopics.Telemetry, dto)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR telemetry broadcast failed.");
            }
        });
    }
}
