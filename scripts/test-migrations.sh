#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCHEMA_FILE="$ROOT_DIR/database/schema/001_initial_schema.sql"

if [[ ! -f "$SCHEMA_FILE" ]]; then
  echo "Schema file not found: $SCHEMA_FILE" >&2
  exit 1
fi

if ! command -v psql >/dev/null 2>&1; then
  echo "psql is required but not found in PATH" >&2
  exit 1
fi

REPLAY=false
if [[ "${1:-}" == "--replay" ]]; then
  REPLAY=true
fi

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-gastronomiq}"
PGDATABASE="${PGDATABASE:-gastronomiq}"

run_psql() {
  psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -v ON_ERROR_STOP=1 "$@"
}

reset_schema() {
  run_psql -c "DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;"
}

verify_tables() {
  run_psql -c "DO \$\$ BEGIN IF to_regclass('public.idempotency_records') IS NULL THEN RAISE EXCEPTION 'idempotency_records table is missing'; END IF; IF to_regclass('public.audit_events') IS NULL THEN RAISE EXCEPTION 'audit_events table is missing'; END IF; END \$\$;"
}

reset_schema
run_psql -f "$SCHEMA_FILE"
verify_tables

if [[ "$REPLAY" == "true" ]]; then
  run_psql -f "$SCHEMA_FILE"
  verify_tables
fi

echo "Migration check passed (replay=$REPLAY)."
