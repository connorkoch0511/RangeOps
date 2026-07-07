# sensor-sim (C)

A minimal telemetry source, written in **C** with POSIX sockets. It models a
climbing test aircraft and streams telemetry to the ground station (any TCP
client) as newline-delimited JSON at 5 Hz.

## Build & run

```bash
make            # produces ./rangeops-sim
./rangeops-sim  # listens on :5555 (override: ./rangeops-sim 6000 or $SIM_PORT)
```

## Protocol

One JSON object per line:

```json
{"alt_ft":12345.6,"airspeed_kt":320.4,"vs_fpm":1800.0,"link_dropout":false}
```

- `alt_ft` — altitude (feet)
- `airspeed_kt` — indicated airspeed (knots)
- `vs_fpm` — vertical speed (feet per minute)
- `link_dropout` — `true` while the telemetry data-link is dropped (values are
  last-known-good, held stale)

## Data-link dropout injection

Between t≈8 s and t≈14 s the sim injects a **telemetry data-link dropout**: the
ground station receives no fresh data, so every channel (altitude, airspeed,
vertical speed) holds its last-known-good value and the samples are flagged
`link_dropout: true`. When the link recovers, the values jump back to the
current flight state. The operator console detects the dropout and flags the
affected samples in the database — the kind of comms/link outage a real test
range must detect and annotate in its recorded data.

## Design notes

- `SIGPIPE` is ignored so a client disconnecting mid-stream doesn't kill the
  server; the accept loop simply waits for the next client.
- `SIGINT` (Ctrl-C) triggers a clean shutdown.
- Each new connection replays a fresh flight profile from t=0.
