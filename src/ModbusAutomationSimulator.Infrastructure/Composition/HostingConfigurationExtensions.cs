using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModbusAutomationSimulator.Core.Contracts;
using ModbusAutomationSimulator.Core.Domain;
using ModbusAutomationSimulator.Core.Simulation;

namespace ModbusAutomationSimulator.Infrastructure.Composition;

/// <summary>Espone factory DI trasversali per simulatori di cella industriale.</summary>
public static class HostingConfigurationExtensions
{
    /// <summary>Collega configurazione (<c>MachineSimulation</c>) -> dominio orchestratore singleton.</summary>
    public static IServiceCollection AddProductionCellOrchestration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        MachineSimulationOptions options = new MachineSimulationOptions();
        configuration.Bind("MachineSimulation", options);

        services.AddSingleton(options);

        services.AddSingleton<ProductionCellOrchestrator>();
        services.AddSingleton<IProductionCellOrchestrator>(sp =>
            sp.GetRequiredService<ProductionCellOrchestrator>());

        return services;
    }
}
