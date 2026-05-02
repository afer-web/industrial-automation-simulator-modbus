using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModbusAutomationSimulator.Dashboard.ViewModels;
using ModbusAutomationSimulator.Infrastructure.Composition;
using ModbusAutomationSimulator.Infrastructure.Logging;
using ModbusAutomationSimulator.Modbus.Composition;

namespace ModbusAutomationSimulator.Dashboard;

public partial class App : Application
{
    IHost? _host;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(e.Args);

        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

        builder.Logging.AddIndustrialSerilog(builder.Configuration);

        builder.Services.AddProductionCellOrchestration(builder.Configuration);
        builder.Services.AddIndustrialModbusFieldbus(builder.Configuration);
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>(sp =>
        {
            MainWindow shell = new();
            shell.DataContext = sp.GetRequiredService<MainViewModel>();
            return shell;
        });

        _host = builder.Build();
        await _host.StartAsync().ConfigureAwait(true);

        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    /// <inheritdoc />
    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        try
        {
            _host?.Services.GetService<MainViewModel>()?.Dispose();

            if (_host is not null)
                await _host.StopAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(true);
        }

        finally
        {
            _host?.Dispose();
        }
    }
}
