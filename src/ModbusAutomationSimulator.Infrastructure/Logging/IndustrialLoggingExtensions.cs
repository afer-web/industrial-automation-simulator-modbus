using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace ModbusAutomationSimulator.Infrastructure.Logging;

/// <summary>Espone configurazione Serilog riutilizzabile (console sintetica, override via callback).</summary>
public static class IndustrialLoggingExtensions
{
    /// <summary>
    /// Registra Serilog come sink principale. Il parametro <paramref name="configuration"/> è disponibile per
    /// personalizzazioni future (es. integrare <see cref="LoggerConfiguration"/> con variabili d'ambiente).
    /// </summary>
    public static void AddIndustrialSerilog(this ILoggingBuilder loggingBuilder,
        IConfiguration configuration,
        Action<LoggerConfiguration>? configure = null)
    {
        _ = configuration;

        LoggerConfiguration lc = BuildDefaultInfrastructureLogger();
        configure?.Invoke(lc);

        Serilog.Core.Logger logger = lc.CreateLogger();
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(logger, dispose: true);
    }

    internal static LoggerConfiguration BuildDefaultInfrastructureLogger(LogEventLevel min = LogEventLevel.Debug)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(min)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate:
                "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
    }
}
