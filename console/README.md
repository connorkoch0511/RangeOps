# console (C# / .NET / Avalonia / EF Core)

The operator console — a desktop app that schedules test missions and captures
live telemetry from the C sensor-sim, persisting everything to the shared
PostgreSQL database via **Entity Framework Core**.

## Projects

| Project | What it is |
|---------|-----------|
| `RangeOps.Core` | Class library: EF Core entities, `RangeOpsContext` (database-first), and the TCP telemetry client/parser. No UI — fully unit-testable. |
| `RangeOps.Console` | Avalonia desktop app (MVVM). The operator's UI. |
| `RangeOps.Tests` | xUnit tests: telemetry parsing (pure) + an ORM round-trip against the real schema. |

## WPF ↔ Avalonia

This is written with **Avalonia** so it builds on macOS/Linux as well as
Windows, but the model is identical to **WPF**: XAML views, a `DataContext`,
`{Binding}` expressions, `ObservableObject`/`ObservableProperty`,
`ObservableCollection`, and `RelayCommand`. The view (`Views/MainWindow.axaml`)
contains no logic — everything lives in `ViewModels/MainWindowViewModel.cs`, the
same separation WPF encourages. A WPF port would reuse `RangeOps.Core` and the
view-model unchanged.

## Run

```bash
docker compose up -d db                 # shared database (repo root)
cd ../sensor-sim && make && ./rangeops-sim &   # telemetry source on :5555
cd ../console
dotnet run --project RangeOps.Console
```

In the app:
1. Pick a mission on the left (or schedule a new one — it's written to Postgres).
2. Select a test run on the right, click **Start capture**.
3. Live altitude/airspeed/vertical-speed update at 5 Hz; when the sim injects a
   stuck-altimeter fault, the **SENSOR FAULT DETECTED** banner lights and the
   faulted samples are flagged in the database.
4. **Stop** ends the run — it's marked `PASS` (clean) or `FAIL` (faults seen),
   and the whole capture is visible in the Django dashboard.

## Tests

```bash
dotnet test          # requires the docker-compose Postgres running
```
