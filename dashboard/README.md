# dashboard (Python / Django)

A read-only web dashboard over the shared RangeOps database, built with
**Django** and the **Django ORM**. It reports on the missions, test runs, and
telemetry that the C# operator console writes.

## Run

```bash
docker compose up -d db     # from repo root, if not already running
./run.sh                    # creates .venv, installs deps, serves :8000
# → http://localhost:8000
```

`run.sh` reads DB settings from a repo-level `.env` if present; otherwise it
uses the docker-compose defaults (`localhost:5544`).

## What it shows

- **Schedule board** (`/`) — every mission with status and a test-run count.
- **Mission detail** (`/missions/<id>/`) — the test runs under a mission.
- **Run detail** (`/runs/<id>/`) — a telemetry report with a max-altitude /
  max-airspeed / fault-count summary and a dependency-free canvas chart that
  marks fault-injected samples in red.

## Design: database-first ORM

The models in `ops/models.py` are declared `managed = False`. Django does **not**
own these tables — `db/schema.sql` does, and the C# console (EF Core) maps to
the same tables. Django is purely a reporting view over the shared operational
database. This mirrors real integration work where multiple systems share one
authoritative schema.

## Tests

```bash
. .venv/bin/activate
POSTGRES_PORT=5544 python -m pytest
```

`conftest.py` loads `db/schema.sql` into the test database (needed because the
models are unmanaged), so tests run against the real schema. Coverage includes
a pure unit test, count roll-ups, and the fault-sample aggregation.
