# GastronomIQ RC8 — Acceptance Harness

This suite is designed to run against a disposable PostgreSQL database.

## Gates

A release candidate must pass all gates:

1. Build / compile
2. Database migration from empty state
3. Migration replay
4. Tenant isolation
5. Idempotency
6. Audit persistence
7. Inventory transaction
8. Procurement → Inventory
9. Production → Inventory
10. Menu Engineering
11. Reporting
12. Rollback integrity
13. Cross-domain end-to-end workflow

The current ChatGPT execution environment does not provide the .NET SDK or a PostgreSQL runtime, so these tests are **prepared but not executed here**.
