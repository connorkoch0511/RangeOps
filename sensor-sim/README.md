# sensor-sim (C)

A minimal instrumentation rig, written in **C** with POSIX sockets. It models a
climbing test aircraft and streams telemetry to any TCP client as
newline-delimited JSON at 5 Hz.

## Build & run

```bash
make            # produces ./rangeops-sim
./rangeops-sim  # listens on :5555 (override: ./rangeops-sim 6000 or $SIM_PORT)
```

## Protocol

One JSON object per line:

```json
{"alt_ft":12345.6,"airspeed_kt":320.4,"vs_fpm":1800.0,"fault":false}
```

- `alt_ft` — altimeter-reported altitude (feet)
- `airspeed_kt` — indicated airspeed (knots)
- `vs_fpm` — vertical speed (feet per minute)
- `fault` — `true` while a **stuck-altimeter fault** is injected

## Fault injection

Between t≈8 s and t≈14 s the sim freezes the reported altitude while the true
aircraft keeps climbing — a classic "stuck altimeter." The operator console
detects this (`fault: true`) and flags the affected telemetry samples in the
database. This is the same fault-injection idea used in HIL/SIL flight-test
rigs to verify that monitoring software catches sensor failures.

## Design notes

- `SIGPIPE` is ignored so a client disconnecting mid-stream doesn't kill the
  server; the accept loop simply waits for the next client.
- `SIGINT` (Ctrl-C) triggers a clean shutdown.
- Each new connection replays a fresh flight profile from t=0.
