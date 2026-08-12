#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

requested_gate=""
if [[ "${1:-}" == "--gate" ]]; then
  requested_gate="${2:-}"
  if [[ -z "$requested_gate" ]]; then
    echo "--gate requires a gate id" >&2
    exit 1
  fi
fi

run_gate() {
  local gate_id="$1"
  local min_tests="$2"
  local command="$3"

  if [[ -n "$requested_gate" && "$requested_gate" != "$gate_id" ]]; then
    return
  fi

  echo "==> [$gate_id] $command"
  local output_file
  output_file="$(mktemp)"
  if ! (cd "$ROOT_DIR" && bash -lc "$command") | tee "$output_file"; then
    rm -f "$output_file"
    echo "Gate failed: $gate_id" >&2
    exit 1
  fi

  if (( min_tests > 0 )); then
    local total_tests
    total_tests="$(
      {
        grep -Eo 'Total tests: [0-9]+' "$output_file" | awk '{print $3}'
        grep -Eo 'Total: *[0-9]+' "$output_file" | awk -F':' '{gsub(/ /, "", $2); print $2}'
      } | awk '{sum += $1} END {print sum+0}'
    )"
    if (( total_tests < min_tests )); then
      rm -f "$output_file"
      echo "Gate failed: $gate_id executed $total_tests tests (minimum $min_tests)." >&2
      exit 1
    fi
  fi

  rm -f "$output_file"
}

run_gate "BUILD" 0 "dotnet build GastronomIQ.sln --configuration Release"
run_gate "UNIT" 1 "dotnet test GastronomIQ.sln --configuration Release --no-build"
run_gate "MIGRATE_EMPTY" 0 "bash ./scripts/test-migrations.sh"
run_gate "MIGRATE_REPLAY" 0 "bash ./scripts/test-migrations.sh --replay"
run_gate "TENANT" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~PlatformHardeningTests\""
run_gate "IDEMPOTENCY" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~IdempotencyMiddlewareTests\""
run_gate "AUDIT" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~AuditWriterTests\""
run_gate "INVENTORY" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~InventoryTests\""
run_gate "PROCUREMENT" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~ProcurementTests\""
run_gate "PRODUCTION" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~ProductionTests\""
run_gate "MENU" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~MenuEngineeringTests\""
run_gate "REPORTING" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~ReportingTests\""
run_gate "ROLLBACK" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~RollbackGuardTests\""
run_gate "E2E" 1 "dotnet test GastronomIQ.Domain.Tests/GastronomIQ.Domain.Tests.csproj --configuration Release --no-build --filter \"FullyQualifiedName~EndToEndWorkflowTests\""

if [[ -n "$requested_gate" ]]; then
  echo "Gate passed: $requested_gate"
else
  echo "All release gates passed."
fi
