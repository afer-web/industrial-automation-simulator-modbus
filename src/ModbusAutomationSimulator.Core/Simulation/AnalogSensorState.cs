namespace ModbusAutomationSimulator.Core.Simulation;

/// <summary>
/// Ingressi analogici simulati (letti come input registers).</summary>
public readonly record struct AnalogSensorState(
    ushort ConveyorSpeedPercent,      // ×0.1 % se preferite finezza
    ushort StationTemperatureC,     // fictitious
    ushort HydraulicPressureBar);      // fictitious ×0.1
