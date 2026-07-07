-- RangeOps shared schema
-- =====================================================================
-- This SQL file is the single source of truth for the database. Both the
-- C# operator console (EF Core) and the Django dashboard (Django ORM) map
-- to these tables "database-first" -- neither ORM owns migrations, so the
-- schema stays consistent across two languages and two runtimes.
--
-- Applied automatically by Postgres on first container start
-- (mounted into /docker-entrypoint-initdb.d by docker-compose).
-- =====================================================================

CREATE TABLE IF NOT EXISTS missions (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(120)  NOT NULL,
    aircraft        VARCHAR(60)   NOT NULL,
    -- planned range/test window
    scheduled_start TIMESTAMPTZ   NOT NULL,
    scheduled_end   TIMESTAMPTZ   NOT NULL,
    -- PLANNED | ACTIVE | COMPLETE | SCRUBBED
    status          VARCHAR(20)   NOT NULL DEFAULT 'PLANNED',
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    CONSTRAINT chk_mission_window CHECK (scheduled_end > scheduled_start)
);

CREATE TABLE IF NOT EXISTS test_runs (
    id           SERIAL PRIMARY KEY,
    mission_id   INTEGER      NOT NULL REFERENCES missions(id) ON DELETE CASCADE,
    name         VARCHAR(120) NOT NULL,
    -- PENDING | RUNNING | PASS | FAIL
    status       VARCHAR(20)  NOT NULL DEFAULT 'PENDING',
    started_at   TIMESTAMPTZ,
    ended_at     TIMESTAMPTZ,
    notes        TEXT
);

CREATE INDEX IF NOT EXISTS idx_test_runs_mission ON test_runs(mission_id);

CREATE TABLE IF NOT EXISTS telemetry_samples (
    id                 BIGSERIAL PRIMARY KEY,
    test_run_id        INTEGER     NOT NULL REFERENCES test_runs(id) ON DELETE CASCADE,
    sample_ts          TIMESTAMPTZ NOT NULL DEFAULT now(),
    altitude_ft        REAL        NOT NULL,
    airspeed_kt        REAL        NOT NULL,
    vertical_speed_fpm REAL        NOT NULL,
    -- true = this sample arrived during a telemetry data-link dropout
    -- (values are last-known-good, held stale until the link recovers)
    link_dropout       BOOLEAN     NOT NULL DEFAULT false
);

CREATE INDEX IF NOT EXISTS idx_telemetry_run_ts
    ON telemetry_samples(test_run_id, sample_ts);
