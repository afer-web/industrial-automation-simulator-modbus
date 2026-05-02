using FluentModbus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModbusAutomationSimulator.Modbus.Configuration;
using ModbusAutomationSimulator.Modbus.Hosting;

namespace ModbusAutomationSimulator.Modbus.Composition;

/// <summary>Registra FluentModbus <see cref="ModbusTcpServer"/> + ciclo RTS integrato.</summary>
public static class IndustrialModbusServiceExtensions
{
    /// <summary>Legge configurazione dalla sezione <c>IndustrialModbus</c>.</summary>
    public static IServiceCollection AddIndustrialModbusFieldbus(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<IndustrialModbusOptions>(
            configuration.GetSection("IndustrialModbus"));

        services.TryAddSingleton(_ =>
            new ModbusTcpServer(isAsynchronous: true));

        services.AddHostedService<IndustrialModbusHostedService>();

        return services;
    }
}
