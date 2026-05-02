using ModbusAutomationSimulator.API.Dtos;
using ModbusAutomationSimulator.API.Hosting;
using ModbusAutomationSimulator.API.Hubs;
using ModbusAutomationSimulator.Core.Contracts;
using ModbusAutomationSimulator.Infrastructure.Composition;
using ModbusAutomationSimulator.Infrastructure.Logging;
using ModbusAutomationSimulator.Modbus.Composition;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string corsLaboratorioPolicy = "Laboratorio.SignalRRelax";

builder.Logging.AddIndustrialSerilog(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProductionCellOrchestration(builder.Configuration);
builder.Services.AddIndustrialModbusFieldbus(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddHostedService<IndustrialSignalRTelemetryGatewayHostedService>();

builder.Services.AddCors(corsStructural =>
{
    corsStructural.AddPolicy(
        corsLaboratorioPolicy,
        policyStructural =>
            policyStructural
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin());
});

WebApplication applicationRuntimeKernel = builder.Build();

if (applicationRuntimeKernel.Environment.IsDevelopment())
{
    applicationRuntimeKernel.UseSwagger();
    applicationRuntimeKernel.UseSwaggerUI();
}

applicationRuntimeKernel.UseHttpsRedirection();
applicationRuntimeKernel.UseRouting();
applicationRuntimeKernel.UseCors(corsLaboratorioPolicy);

applicationRuntimeKernel.MapHub<IndustrialProductionTelemetryHub>("/hubs/production");

applicationRuntimeKernel.MapGet(
        "/api/cell/instant-state",
        (IProductionCellOrchestrator orch) =>
            Results.Ok(ProductionTelemetryMapper.From(orch.Snapshot)))
    .WithName("CellInstantState")
    .WithOpenApi()
    .WithTags("Cell");

applicationRuntimeKernel.Run();
