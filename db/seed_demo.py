"""
Seed a RangeOps database with a representative demo dataset.

Loads db/schema.sql, then inserts a few missions, test runs, and a full
telemetry capture (with an injected data-link dropout window) so the web
dashboard has realistic data to show -- including the dropout-marked chart.

Usage:
    DATABASE_URL=postgres://... python db/seed_demo.py

Reads DATABASE_URL_UNPOOLED first (better for DDL), falling back to DATABASE_URL
or POSTGRES_URL.
"""
import os
import pathlib
from datetime import datetime, timedelta, timezone

import psycopg

HERE = pathlib.Path(__file__).resolve().parent
SCHEMA = HERE / "schema.sql"


def conn_string() -> str:
    for key in ("DATABASE_URL_UNPOOLED", "POSTGRES_URL_NON_POOLING",
                "DATABASE_URL", "POSTGRES_URL"):
        if os.environ.get(key):
            return os.environ[key]
    # Fall back to discrete vars (local docker / CI Postgres, no SSL).
    host = os.environ.get("POSTGRES_HOST", "localhost")
    port = os.environ.get("POSTGRES_PORT", "5432")
    db = os.environ.get("POSTGRES_DB", "rangeops")
    user = os.environ.get("POSTGRES_USER", "rangeops")
    pw = os.environ.get("POSTGRES_PASSWORD", "rangeops")
    return f"host={host} port={port} dbname={db} user={user} password={pw}"


def make_climb(n_samples: int, dropout_from: int, dropout_to: int):
    """Generate a climbing-telemetry profile with a data-link dropout window,
    mirroring what the C sensor-sim produces: during the dropout the ground
    station holds the last-known-good sample (all channels stale)."""
    alt = 0.0
    airspeed = 140.0
    rx = (0.0, 140.0, 0.0)  # last received (alt, airspeed, vs); held on dropout
    rows = []
    for i in range(n_samples):
        vs = 2500.0
        alt += vs / 60.0 / 5.0            # 5 Hz
        airspeed = min(320.0, airspeed + 0.5)
        dropout = dropout_from <= i < dropout_to
        if not dropout:
            rx = (alt, airspeed, vs)      # link up: receive current state
        r_alt, r_ias, r_vs = rx           # during dropout these stay stale
        rows.append((r_alt, r_ias, r_vs, dropout))
    return rows


def main() -> None:
    schema_sql = SCHEMA.read_text()
    now = datetime.now(timezone.utc)

    with psycopg.connect(conn_string(), autocommit=True) as conn, conn.cursor() as cur:
        # Drop first so schema changes (e.g. renamed columns) always take, then
        # recreate from the shared schema. Safe: these are demo databases.
        cur.execute("DROP TABLE IF EXISTS telemetry_samples, test_runs, missions CASCADE;")
        cur.execute(schema_sql)

        # --- missions ---
        cur.execute(
            """INSERT INTO missions(name,aircraft,scheduled_start,scheduled_end,status)
               VALUES (%s,%s,%s,%s,%s) RETURNING id""",
            ("Envelope Expansion 4A", "F-16C", now - timedelta(hours=2),
             now - timedelta(hours=1), "COMPLETE"))
        m1 = cur.fetchone()[0]

        cur.execute(
            """INSERT INTO missions(name,aircraft,scheduled_start,scheduled_end,status)
               VALUES (%s,%s,%s,%s,%s)""",
            ("Stores Separation 12", "F/A-18E", now + timedelta(days=1),
             now + timedelta(days=1, hours=2), "PLANNED"))

        cur.execute(
            """INSERT INTO missions(name,aircraft,scheduled_start,scheduled_end,status)
               VALUES (%s,%s,%s,%s,%s) RETURNING id""",
            ("Avionics Regression 7", "T-38C", now,
             now + timedelta(minutes=90), "ACTIVE"))
        m3 = cur.fetchone()[0]

        # --- a clean run (no dropouts) ---
        cur.execute(
            """INSERT INTO test_runs(mission_id,name,status,started_at,ended_at,notes)
               VALUES (%s,%s,%s,%s,%s,%s)""",
            (m1, "Level accel M0.9", "PASS", now - timedelta(hours=2),
             now - timedelta(hours=2, minutes=-8), "Nominal"))

        # --- the captured run WITH a data-link dropout (drives the chart) ---
        cur.execute(
            """INSERT INTO test_runs(mission_id,name,status,started_at,ended_at,notes)
               VALUES (%s,%s,%s,%s,%s,%s) RETURNING id""",
            (m1, "Climb to FL250", "FAIL", now - timedelta(minutes=50),
             now - timedelta(minutes=49), "Data-link dropout t=12-20s (telemetry held stale)"))
        run_id = cur.fetchone()[0]

        samples = make_climb(n_samples=180, dropout_from=60, dropout_to=100)
        base = now - timedelta(minutes=50)
        with cur.copy(
            "COPY telemetry_samples "
            "(test_run_id, sample_ts, altitude_ft, airspeed_kt, vertical_speed_fpm, link_dropout) "
            "FROM STDIN"
        ) as copy:
            for i, (alt, ias, vs, dropout) in enumerate(samples):
                copy.write_row((run_id, base + timedelta(milliseconds=200 * i),
                                alt, ias, vs, dropout))

        # --- an in-progress run on the active mission ---
        cur.execute(
            """INSERT INTO test_runs(mission_id,name,status,started_at)
               VALUES (%s,%s,%s,%s)""",
            (m3, "Nav database load", "RUNNING", now - timedelta(minutes=5)))

        cur.execute("SELECT count(*) FROM telemetry_samples WHERE link_dropout;")
        dropouts = cur.fetchone()[0]

    print(f"Seeded: 3 missions, 3 test runs, {len(samples)} telemetry samples "
          f"({dropouts} flagged as data-link dropouts).")


if __name__ == "__main__":
    main()
