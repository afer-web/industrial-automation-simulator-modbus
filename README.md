# Industrial-automation-simulator-modbus
Simulazione completa di una piccola unità di produzione industriale, comprensiva di nastro trasportatore, unità di elaborazione, controllo qualità e magazzino. Espone gli stati della macchina, i sensori, gli attuatori, gli allarmi e i contatori tramite un server Modbus TCP simulato. Include una dashboard in tempo reale realizzata con WPF e un backend .NET basato su Clean Architecture.

## FC01 — Read Coils (lettura/uscite PLC simulatore + stessa area per comando da master)

Su questa slave le **bobine** sono **un unico buffer**: contengono sia le **uscite** emulate (primitive indirizzi 0…) sia i **comandi** supervisor (dal conf. indirizzo **96**).  
Puoi leggere tutto con **FC01**. Le bobine comando **FC05** vengono azzerate dall’RTS dopo il consumo (impulsi).

### Uscite / indicazioni (simulate)

| Offset PDU | Alias Modicon coils | Funzione sintetica        | Interpretazione lettura                      |
|-----------:|---------------------|---------------------------|----------------------------------------------|
| 0          | 00001               | Conveyor motor ON         | 1 = motore traslazione attivo                |
| 1          | 00002               | Stazione hydraulic/clamp  | 1 = idraulica/braccio attivi                 |
| 2          | 00003               | Scarto diverter           | 1 = deviatore scarto ON                      |
| 3          | 00004               | Torretta verde            | 1 = stato “OK/laboratorio idle” sintetizzato |
| 4          | 00005               | Torretta ambra            | 1 = ciclo / stato intermedio                 |
| 5          | 00006               | Torretta rossa            | 1 = presenza alarm/fault sintetizzato        |

### Comandi verso simulator (SCR/HMI → scrittura FC05 sugli offset sotto)

*Usare **`1`** sul bit e attendere handshake: molte bobine vengono **reset a 0** dal software dopo elaborazione.*

| Offset PDU | Alias Modicon | Comando                                      |
|-----------:|---------------|----------------------------------------------|
| 96         | 00097         | START automatico (impulso)                   |
| 97         | 00098         | STOP morbido (**mantenuto** mentre =1)       |
| 98         | 00099         | Emergency stop (**mantenuto** mentre =1)     |
| 99         | 00100         | Reset sicurezza / fault (impulso)            |
| 100        | 00101         | Fault injection (impulso)                    |
| 101        | 00102         | Ack allarmi (impulso)                        |


---

## FC02 — Read Discrete Inputs (ingressi / sensori simulati)

| Offset PDU | Alias Modicon DI | Sensore                                      |
|-----------:|------------------|----------------------------------------------|
| 0          | 10001            | Feedback nastro ON (motor running)           |
| 1          | 10002            | Pezzo zona ingresso nastro                   |
| 2          | 10003            | Pezzo zona stazione                          |
| 3          | 10004            | Sensore clamp chiuso / agganciamento         |
| 4          | 10005            | Lane misura QC attiva                        |
| 5          | 10006            | Lane magazzino uscita ready                  |

---

## FC04 — Read Input Registers (16 bit, read-only — analog sintetizzati)

| Offset PDU | Alias Modicon IR | Scala / sintesi della lettura                                      |
|-----------:|------------------|--------------------------------------------------------------------|
| 0          | 30001            | Velocità nastro: **`0 … 1000`** ≈ premille (**1000 ≈ 100%**)       |
| 1          | 30002            | Temperatura stazione: **valore_UI / 10** ≈ °C (campo sintetico)    |
| 2          | 30003            | Pressione idraulica: **valore_UI / 10** ≈ bar (campo sintetico)    |

---

## FC03 / FC16 — Holding Registers (stato ciclo / contatori / allarmi)

Tutti i word **16-bit**. I contatori **`uint32`** usano **due registri LE**: **Low word prima, High word dopo** (`Lo` indirizzo N, `Hi` indirizzo N+1).

**Valore 32-bit** = `Lo + (Hi << 16)` (interpretazione little-endian a livello di word).

| Offset PDU | Alias Modicon HR | Contenuto                                                |
|-----------:|------------------|----------------------------------------------------------|
| 0          | 40001            | Stato macchina (`MachineState` enum come `ushort`)       |
| 1          | 40002            | Bitmask stato (allarmi, ciclo, torretta, halt, E-Stop…)  |
| 2          | 40003            | Contatore **pezzi OK** — **WORD bassa** (`uint32` LE)    |
| 3          | 40004            | Contatore **pezzi OK** — **WORD alta**                   |
| 4          | 40005            | Contatore **scarti** — **WORD bassa**                    |
| 5          | 40006            | Contatore **scarti** — **WORD alta**                     |
| 6          | 40007            | Tempo **fase corrente** (ms, saturazione `ushort`)       |
| 7          | 40008            | Tempo da **tick ciclo cumulativo** (ms aggregato sintet.)|
| 8          | 40009            | Codice **allarme PLC** più rilevante (0 = assente; vedi enum `AlarmCode` nel Core). |

---

# Verifica registri Modbus

Collegamento allo **slave TCP** configurato nell’app (Dashboard/API), sezione **`IndustrialModbus`** in `appsettings.json`:

| Parametro       | Default tipico                         |
|---------------- |----------------------------------------|
| Protocollo      | Modbus TCP / RTU-over-TCP (**TCP**)    |
| Indirizzo IP    | localhost o IP della macchina          |
| Porta TCP       | **`502`** (se non modificata in config)|
| Slave / Unit ID | **`0`** (unità singola FluentModbus)   |

Nei simulatori Modbus (**Modbus Poll**, QmodMaster, ecc.) scegliere la **funzione** corretta (`01`, `02`, `03`, `04`), poi il **punto di inizio**.  
Due convenzioni comuni:

- **Offset 0 nel PDU** (“address” = primo elemento a **0**) — compatibile col codice e con molti client se imposti “PLC addressing” **Base 0**.
- Numerazione **stile Modicon** (opzionale): bobina **`00001`** = offset **0**, holding **`40001`** = offset **0**, ecc.

Di seguito **offset 0-based** (PDU), come nei costanti interni (`IndustrialModbusAddressPlan`).  
*Nella Tabella viene indicata anche la notazione coils `0xxxx` / input `1xxxx` / input reg `3xxxx` / holding `4xxxx` solo come riferimento tipico.*

---

