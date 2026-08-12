# RC8.2 CI Validation — Issue #1 Tracking Document

> **GitHub Issue:** [#1 — RC8.2 CI Validation: Resolve compile/test failures and achieve green pipeline](../../issues/1)
> **Related PRs:** [#2](../../pull/2) · [#3](../../pull/3)
> **Status:** Open — pending first CI green run

---

## Description

GastronomIQ is currently in **RC8.2** phase. All domain layers (Inventory, Procurement, Production, Menu Engineering, Reporting) are implemented and the PostgreSQL 16 + .NET 8 CI pipeline is scaffolded. The next critical milestone is to trigger the RC8.2 CI workflow and fix any compilation or test failures until the pipeline is fully green.

## Motivation

Without a passing CI pipeline, no RC8.2 release candidate can be promoted to a stable release. This document tracks the work needed to reach a consistently green CI state, giving the team confidence that all domain layers integrate correctly and the database migration baseline is sound.

## Proposed Solution

1. Trigger the GitHub Actions CI workflow (defined in `.github/workflows/ci.yml`) on the `main` branch and on related scaffolding PRs.
2. Triage every failing step (restore → build → test → migration smoke) and apply targeted fixes.
3. Iterate until all CI gates pass on every subsequent push.

## Acceptance Criteria

- [ ] `dotnet restore GastronomIQ.sln` completes with no errors
- [ ] `dotnet build GastronomIQ.sln --configuration Release --no-restore` completes with zero warnings treated as errors
- [ ] `dotnet test GastronomIQ.sln --configuration Release --no-build` passes all unit tests (Domain.Tests + Application.Tests)
- [ ] Migration smoke test applies `database/schema/001_initial_schema.sql` to a PostgreSQL 16 instance without error
- [ ] Migration smoke test confirms `idempotency_records` and `audit_events` tables exist post-migration
- [ ] The full CI workflow completes green on a push to `main`
- [ ] No regressions in any previously passing test

## Use Cases

- Developers can open a PR and get immediate, automated feedback on build/test health.
- Release manager can verify RC8.2 candidate integrity before promotion.

## Alternative Solutions

- Running builds locally only — not acceptable for a team workflow; CI is required for RC8.2.

## Impact

| Dimension            | Rating |
|----------------------|--------|
| User Impact          | High — a green pipeline unblocks all subsequent releases |
| Technical Complexity | Medium — diagnosing compile errors, missing project references, migration scripts |
| Effort Estimate      | 4–8 hours depending on the number of failures discovered |

## Related Work

| PR | Title | Status |
|----|-------|--------|
| [#2](../../pull/2) | [WIP] Add new feature with proposed solution | Draft — solution file, test projects, migrations, CI workflow |
| [#3](../../pull/3) | RC8.2: Add CI solution/test scaffolding, baseline migration, and GitHub Actions pipeline | Draft — ordered CI gates (restore → build → test → migration smoke) |

Once one of these PRs is merged and CI is green, Issue #1 can be closed.

## RC8.2 Readiness Checklist

- [x] Domain layer — Inventory
- [x] Domain layer — Procurement
- [x] Domain layer — Production
- [x] Domain layer — Menu Engineering
- [x] Domain layer — Reporting
- [x] PostgreSQL 16 CI service configured
- [x] .NET 8 build pipeline defined
- [x] Solution file (`GastronomIQ.sln`) with all projects included
- [x] xUnit test projects (`Domain.Tests`, `Application.Tests`)
- [x] Baseline migration SQL (`database/schema/001_initial_schema.sql`)
- [ ] **CI pipeline green on `main`** ← _current blocker_
