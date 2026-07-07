#!/usr/bin/env bash
# End-to-end system test for RangeOps:
#   C sensor-sim  ->  C#/EF Core capture CLI  ->  PostgreSQL  ->  (queried back)
#
# Proves the whole pipeline works across C, C#, and SQL against the shared
# schema. Requires: docker-compose Postgres running, .NET SDK, a C compiler.
set -euo pipefail
cd "$(dirname "$0")/.."

export POSTGRES_PORT="${POSTGRES_PORT:-5544}"
export SIM_PORT="${SIM_PORT:-5555}"
SAMPLES="${1:-90}"    # 90 @ 5 Hz = 18 s, covers the t=8-14 s fault window

psql_q() { docker exec -i rangeops-db psql -U rangeops -d rangeops -qtA -c "$1"; }
fail()   { echo "SYSTEM TEST FAILED: $1" >&2; exit 1; }

echo "- building sensor-sim and capture CLI"
make -C sensor-sim >/dev/null
dotnet build console/RangeOps.Capture/RangeOps.Capture.csproj -v q --nologo >/dev/null

echo "- starting sensor-sim on :$SIM_PORT"
./sensor-sim/rangeops-sim "$SIM_PORT" >/tmp/rangeops-systest-sim.log 2>&1 &
SIM=$!
trap 'kill $SIM 2>/dev/null || true' EXIT
sleep 1

echo "- scheduling a mission + test run"
psql_q "DELETE FROM missions WHERE name='System Test';" >/dev/null
RUNID=$(psql_q "WITH m AS (
           INSERT INTO missions(name,aircraft,scheduled_start,scheduled_end,status)
           VALUES('System Test','F-16C',now(),now()+interval '1 hour','ACTIVE') RETURNING id)
         INSERT INTO test_runs(mission_id,name,status)
           SELECT id,'Climb capture','PENDING' FROM m RETURNING id;")
[ -n "$RUNID" ] || fail "could not create test run"
echo "  test_run id=$RUNID"

echo "- capturing $SAMPLES telemetry samples"
dotnet run --project console/RangeOps.Capture --no-build -- "$RUNID" "$SAMPLES" >/dev/null

# ---- assertions ----
N=$(psql_q      "SELECT count(*) FROM telemetry_samples WHERE test_run_id=$RUNID;")
FAULTS=$(psql_q "SELECT count(*) FROM telemetry_samples WHERE test_run_id=$RUNID AND fault_injected;")
STATUS=$(psql_q "SELECT status FROM test_runs WHERE id=$RUNID;")

echo "  samples=$N faults=$FAULTS status=$STATUS"
[ "$N" -eq "$SAMPLES" ]   || fail "expected $SAMPLES samples, got $N"
[ "$FAULTS" -gt 0 ]       || fail "expected the injected fault window to be recorded"
[ "$STATUS" = "FAIL" ]    || fail "run should be FAIL after detecting faults, got $STATUS"

echo "SYSTEM TEST PASSED [OK]  (sim -> capture -> Postgres, faults detected end to end)"
