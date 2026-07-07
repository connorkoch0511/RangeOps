#!/usr/bin/env bash
# Start the RangeOps web dashboard against the shared Postgres database.
set -euo pipefail
cd "$(dirname "$0")"

if [ ! -d .venv ]; then
  python3 -m venv .venv
  ./.venv/bin/pip install --quiet -r requirements.txt
fi

# Load shared DB settings if a repo-level .env exists.
[ -f ../.env ] && set -a && . ../.env && set +a

exec ./.venv/bin/python manage.py runserver 0.0.0.0:8000
