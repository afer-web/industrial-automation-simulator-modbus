![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-5C2D91?style=for-the-badge&logo=windows&logoColor=white)
![XAML](https://img.shields.io/badge/XAML-F28C28?style=for-the-badge&logo=xml&logoColor=white)

# Industrial Automation Simulator - Modbus

Complete simulation of a small industrial production unit, including a conveyor belt, processing station, quality control module, and storage area.
It exposes machine states, sensors, actuators, alarms, and counters through a simulated Modbus TCP server.
The project includes a real‑time dashboard built with WPF and a .NET backend following Clean Architecture principles.

<img width="1064" height="733" alt="immagine" src="https://github.com/user-attachments/assets/998a6494-6184-454a-a461-c5813d01685a" />

## FC01 — Read Coils (a unified Modbus memory map that exposes simulated PLC inputs/outputs and also accepts commands from an external master).

On this slave, the coils are exposed as a single contiguous buffer: they contain both the emulated outputs (starting at address 0…) and the supervisor commands (from configured address 96 onward).
The entire area can be read using FC01.
Command coils written via FC05 are automatically reset by the RTS after being consumed (pulse‑type behavior).

Simulated Outputs / Status Indicators

| Bit index  | Coil address        | Description               | Meaning when 1                               |
|-----------:|---------------------|---------------------------|----------------------------------------------|
| 0          | 00001               | Conveyor motor ON         | 1 = conveyor drive active                    |
| 1          | 00002               | Hydraulic / clamp station | 1 = hydraulic unit / clamp arm engaged       |
| 2          | 00003               | Reject diverter           | 1 = reject diverter activated                |
| 3          | 00004               | Green tower light         | 1 = “OK / lab idle” consolidated status      |
| 4          | 00005               | Amber tower light         | 1 = cycle running / intermediate state       |
| 5          | 00006               | Red tower light           | 1 = alarm / fault present (consolidated)     |

### Commands to the simulator (SCR/HMI → FC05 writes to the offsets below)

*Use **`1`** on the bit and wait for the handshake: many command coils are reset to **`0`** by the software after processing*

| Offset PDU | Alias Modicon | Command                                      |
|------------|---------------|----------------------------------------------|
| 96         | 00097         | START automatic (pulse)                      |
| 97         | 00098         | Soft STOP (**held** while =1)                |
| 98         | 00099         | Emergency stop (**held** while =1)           |
| 99         | 00100         | Safety / fault reset (pulse)                 |
| 100        | 00101         | Fault injection (pulse)                      |
| 101        | 00102         | Alarm acknowledge (pulse)                    |

---

## FC02 — Read Discrete Inputs (simulated inputs / sensors)

| Offset PDU | Alias Modicon DI | Sensor                                       |
|------------|------------------|----------------------------------------------|
| 0          | 10001            | Conveyor ON feedback (motor running)         |
| 1          | 10002            | Piece at conveyor entry zone                 |
| 2          | 10003            | Piece at processing station                  |
| 3          | 10004            | Clamp closed / engagement sensor             |
| 4          | 10005            | QC measurement lane active                   |
| 5          | 10006            | Storage lane exit ready                      |

---

## FC04 — Read Input Registers (16 bit, read‑only — synthesized analog values)

| Offset PDU | Alias Modicon IR | Scale / synthesized reading                                   |
|------------|------------------|---------------------------------------------------------------|
| 0          | 30001            | Conveyor speed: **`0 … 1000`** ≈ permille (**1000 ≈ 100%**)   |
| 1          | 30002            | Station temperature: **value_UI / 10** ≈ °C (synthetic field) |
| 2          | 30003            | Hydraulic pressure: **value_UI / 10** ≈ bar (synthetic field) |

---

## FC03 / FC16 — Holding Registers (cycle state / counters / alarms)

All words are **16‑bit**. The **`uint32`** counters use **two LE registers**: **Low word first, High word after** (`Lo` at address N, `Hi` at address N+1).

**32‑bit value** = `Lo + (Hi << 16)` (little‑endian interpretation at word level).

| Offset PDU | Alias Modicon HR | Content                                                                    |
|-----------:|------------------|----------------------------------------------------------------------------|
| 0          | 40001            | Machine state (`MachineState` enum as `ushort`)                            |
| 1          | 40002            | State bitmask (alarms, cycle, tower light, halt, E‑Stop…)                  |
| 2          | 40003            | **OK pieces** counter — **low WORD** (`uint32` LE)                         |
| 3          | 40004            | **OK pieces** counter — **high WORD**                                      |
| 4          | 40005            | **Rejects** counter — **low WORD**                                         |
| 5          | 40006            | **Rejects** counter — **high WORD**                                        |
| 6          | 40007            | **Current phase** time (ms, `ushort` saturation)                           |
| 7          | 40008            | **Cumulative cycle tick** time (aggregated ms, synth.)                     |
| 8          | 40009            | Most relevant **PLC alarm code** (0 = none; see `AlarmCode` enum in Core)  |

---

# Modbus Registers Verification

Connection to the **TCP slave** configured in the app (Dashboard/API), section **`IndustrialModbus`** in `appsettings.json`:

| Parametro       | Typical default                           |
|---------------- |-------------------------------------------|
| Protocollo      | Modbus TCP / RTU-over-TCP (**TCP**)       |
| Indirizzo IP    | localhost or machine IP                   |
| Porta TCP       | **`502`** (if not changed in config)      |
| Slave / Unit ID | **`0`** (single FluentModbus unit)        |

In Modbus testing tools (e.g., **Modbus Poll**, QModMaster, etc.) select the correct **function** (`01`, `02`, `03`, `04`), then the **starting point`.
Two common conventions:

- **Offset 0 in the PDU** (“address” = first element at **0**) — compatible with the code and with many clients if you set “PLC addressing” to **Base 0**.
- **Modicon‑style numbering** (optional): coil **`00001`** = offset **0**, holding **`40001`** = offset **0**, etc.

---

