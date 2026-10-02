# RC8 Rollback Scenarios

## R1 — Procurement receipt
Inject a failure after receipt creation but before inventory movement.

Expected:
- no purchase receipt
- PO received quantity unchanged
- PO status unchanged
- no inventory movement

## R2 — Production completion
Inject a failure after one ingredient consumption but before the final ingredient.

Expected:
- no consumption movements
- no waste
- batch remains INPROGRESS
- inventory unchanged

## R3 — Reporting snapshot
Inject a failure while writing KPI rows.

Expected:
- no orphan dashboard snapshot
- no partial KPI set

## R4 — Duplicate command
Replay an accepted state-changing command using the same Idempotency-Key.

Expected:
- exactly one state transition
- same response
- one audit event
