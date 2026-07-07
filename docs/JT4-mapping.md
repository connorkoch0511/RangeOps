# RangeOps → JT4 "Computer Scientist I" mapping

A cheat-sheet for talking about this project against the JT4 / J-Tech II job
requirements. Every claim points at real code you can open and walk through.

## Requirement coverage

| Job requirement | Where it lives in RangeOps |
|---|---|
| **Python** | `dashboard/` — Django app, views, ORM queries |
| **SQL** | `db/schema.sql` — schema, constraints, indexes; raw SQL in tests/CI |
| **C** | `sensor-sim/sim.c` — POSIX sockets, signal handling, a flight model |
| **C#** | `console/RangeOps.Core` + `RangeOps.Console` + `RangeOps.Capture` |
| **.NET Framework (WPF)** | `console/RangeOps.Console` — Avalonia is WPF-family XAML/MVVM (see console/README.md) |
| **HTML / CSS / JavaScript** | `dashboard/ops/templates/**`, `dashboard/ops/static/ops/chart.js` (dependency-free canvas chart) |
| **Django** | `dashboard/` — models, views, templates, tests |
| **ORMs (Object-Relational Mappers)** | EF Core (`RangeOpsContext`) **and** Django ORM, both database-first on one schema |
| **Unit / integration / system testing** | xUnit (`console/RangeOps.Tests`), pytest (`dashboard/ops/tests.py`), `scripts/system-test.sh` |
| **Systems engineering & SDLC** | Multi-component system, shared schema contract, CI (`.github/workflows/ci.yml`), docs |
| **Scheduling / test-operations automation** (the JD's core duty) | The operator console schedules missions and automates telemetry capture for test runs |
| **Flight-test range operations** | Mission scheduling, test-run data capture, data-link dropout detection, and cross-system reporting — the *operations & integration* layer of a range |

## Architecture decisions worth discussing

- **One schema, two ORMs, database-first.** `db/schema.sql` is the single source
  of truth. Neither EF Core nor Django owns migrations — both map to the existing
  tables. Talk about *why*: when multiple systems (a desktop tool and a web app,
  in different languages) share an operational database, the schema is the
  contract. This is exactly the integration situation a range contractor lives in.

- **Testability through seams.** Capture logic lives in `CaptureService`, which
  depends on an `ITelemetrySource` interface. The real source is a TCP client to
  the C sim; tests inject an in-memory source. Same code path runs in the GUI, the
  CLI, and the tests. Good talking point about designing for test.

- **Data-link dropout detection.** The C telemetry source injects a telemetry
  **data-link dropout** (the ground station holds last-known-good data while the
  link is down); the console detects it, flags the samples, and flags the run for
  review. This is a *range-operations* concern — detecting and annotating comms
  outages in recorded test data — distinct from sensor/SIL-level testing.

- **Complements FlightBench, doesn't duplicate it.** FlightBench is the *test
  engine* (C++ physics + control-law verification, SIL/HIL). RangeOps is the
  *operations & data layer* (scheduling, capture, integration, reporting). Almost
  no stack overlap — together they cover both sides the JD asks for. Say this
  explicitly if asked about the two.

- **Headless + GUI parity.** The `RangeOps.Capture` CLI and the Avalonia console
  both drive `CaptureService`, so the whole pipeline is demonstrable and testable
  without a display — useful in CI and on a server.

## Verified end-to-end

`scripts/system-test.sh` runs the real pipeline and asserts on the result:

```
C sensor-sim  →  C#/EF Core capture  →  PostgreSQL  →  Django dashboard
```

A representative run: **90 telemetry samples captured, 30 data-link dropouts
detected (the injected 6-second link-outage window), run auto-marked FAIL**, and
the same run rendered in the web dashboard with its dropout count highlighted.

## Résumé bullet candidates

- Built a multi-language flight-test operations suite (C, C#/.NET, Python/Django)
  integrating through a shared PostgreSQL schema via two ORMs (EF Core + Django ORM).
- Wrote a C telemetry source (POSIX sockets) streaming 5 Hz test-aircraft telemetry
  with injectable data-link dropouts; built a C#/XAML-MVVM operator console that
  schedules test missions and captures/validates telemetry in real time.
- Established unit, integration, and end-to-end system tests with CI (GitHub Actions),
  including a scripted pipeline test asserting data-link dropout detection across all
  components.

## Honest caveats to be ready for

- **WPF vs Avalonia.** Be upfront: the desktop app uses Avalonia so it builds on
  macOS/Linux, but it's the same C#/XAML/MVVM model as WPF and ports directly. Don't
  claim WPF specifically unless you rebuild it on Windows (easy follow-up — reuse
  `RangeOps.Core` and the view-model).
- **ASP Classic** from the JD is intentionally not here (obsolete, Windows/IIS-only).
  If asked, describe how the Django dashboard is the modern equivalent of a legacy
  ASP reporting page.
