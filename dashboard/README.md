# dashboard (Python / Django)

A read-only web dashboard over the shared RangeOps database, built with
**Django** and the **Django ORM**. It reports on the missions, test runs, and
telemetry that the C# operator console writes.

**Live demo:** https://rangeops-dashboard.vercel.app (deployed on Vercel,
backed by a managed Neon Postgres, seeded with a representative capture).

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
  max-airspeed / link-dropout-count summary and a dependency-free canvas chart
  that shades the data-link dropout window in amber.

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
a pure unit test, count roll-ups, and the link-dropout aggregation.

## End-to-end tests + screenshots

[`e2e/`](e2e/) holds Playwright tests that drive a real browser over the running
dashboard, assert on what a user sees, and capture the screenshots used in the
top-level README (so they can't drift from the real UI).

```bash
pip install -r e2e/requirements.txt
python -m playwright install chromium

# against a local server (seed the DB first with db/seed_demo.py, then ./run.sh)
python -m pytest e2e/

# or against the live deployment
BASE_URL=https://rangeops-dashboard.vercel.app python -m pytest e2e/
```

Playwright deps are kept out of `requirements.txt` so they never ship in the
Vercel serverless bundle.
