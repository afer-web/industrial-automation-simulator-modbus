using System.Net;
using FluentModbus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModbusAutomationSimulator.Core.Contracts;
using ModbusAutomationSimulator.Modbus.Configuration;
using ModbusAutomationSimulator.Modbus.Mapping;

namespace ModbusAutomationSimulator.Modbus.Hosting;

/// <summary>Ciclo di scansione combinato supervisore ↔ orchestratore ↔ mappe Modbus FluentModbus.</summary>
public sealed class IndustrialModbusHostedService(
    ModbusTcpServer tcpServer,
    IProductionCellOrchestrator orchestrator,
    IOptionsMonitor<IndustrialModbusOptions> options,
    ILogger<IndustrialModbusHostedService> logger) : BackgroundService
{
    readonly IndustrialSupervisorLatch _supervisorLatch = new();

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IndustrialModbusOptions bootOptionsSnapshot = options.CurrentValue;

        TimeSpan cadence =
            TimeSpan.FromMilliseconds(Math.Clamp(bootOptionsSnapshot.ScanMilliseconds, 5, 300));

        IPEndPoint endpoint =
            ResolveEndpoint(bootOptionsSnapshot.ListenAddress, bootOptionsSnapshot.Port);

        lock (tcpServer.Lock)
            tcpServer.ClearBuffers(IndustrialModbusMapper.DefaultUnitIdentifier);

        tcpServer.Start(endpoint);

        logger.LogInformation("Modbus TCP slave in ascolto su {Endpoint}; periodo ciclo={Cadence}.",
            endpoint, cadence);

        using PeriodicTimer ticker = new(cadence);

        try
        {
            while (await ticker.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                IndustrialModbusOptions cycleOptionsProbe = options.CurrentValue;

                if (cycleOptionsProbe.VerboseTracing)
                    logger.LogTrace(
                        "[Modbus-scan] stato orchestratore (pre ciclo)= {MachineState}",
                        orchestrator.Snapshot.MachineState);

                try
                {
                    lock (tcpServer.Lock)
                    {
                        IndustrialModbusMapper.ProcessSupervisoryCoils(
                            tcpServer,
                            orchestrator,
                            _supervisorLatch);

                        orchestrator.Tick(cadence);

                        IndustrialModbusMapper.ApplyMachineSnapshot(tcpServer, orchestrator.Snapshot);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Errore ciclo combinato RTS/RTU virtuale.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Shutdown ciclo industriale virtuale.");
        }

        StopServerSafely();
    }

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        StopServerSafely();
        return base.StopAsync(cancellationToken);
    }

    static IPEndPoint ResolveEndpoint(string listenBindingRaw, int portParameter)
    {
        string trimmedListen = listenBindingRaw.Trim();

        IPAddress ipAddress = trimmedListen switch
        {
            "+" or "*" => IPAddress.Any,
            "::" => IPAddress.IPv6Any,
            _ => IPAddress.Parse(trimmedListen),
        };

        ushort tcpPort = unchecked((ushort)Math.Clamp(portParameter, 1, 65535));
        return new IPEndPoint(ipAddress, tcpPort);
    }

    void StopServerSafely()
    {
        try
        {
            tcpServer.Stop();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Errore fermando FluentModbus server TCP.");
        }
    }
}
