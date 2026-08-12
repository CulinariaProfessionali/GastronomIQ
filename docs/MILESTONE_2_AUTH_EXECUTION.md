# Milestone 2 Execution: Authentication & Identity

## Scope lock (Milestone 2)

Milestone 2 delivery is explicitly limited to:

1. Registration
2. Login
3. Refresh token lifecycle (issue, rotate, revoke)
4. Logout/revocation
5. RBAC permission assignment and checks
6. Organization scoping in auth token claims
7. Security hardening for auth input validation and token handling

Items outside this scope move to Milestone 3+ unless they are required to complete the above safely.

## Vertical slices and ownership

Ownership is assigned by role to avoid person-specific coupling.

1. **Auth API endpoints**
   - Owner role: **API Maintainer**
   - Responsibilities: endpoint contracts, status codes, request/response compatibility

2. **Domain/Application auth rules**
   - Owner role: **Identity Maintainer**
   - Responsibilities: registration/login/refresh/logout behavior, validation, token semantics

3. **Infrastructure persistence**
   - Owner role: **Infrastructure Maintainer**
   - Responsibilities: refresh token persistence/revocation strategy, data-layer integration path

4. **Middleware/policies (RBAC + organization scope)**
   - Owner role: **Security Maintainer**
   - Responsibilities: permission extraction, access policy behavior, tenant isolation guardrails

5. **Testing (unit + integration checkpoints)**
   - Owner role: **Quality Maintainer**
   - Responsibilities: test-first checkpoints, regression coverage, test gate enforcement

6. **OpenAPI + docs**
   - Owner role: **Documentation Maintainer**
   - Responsibilities: keep API contract and milestone docs in sync with implementation

## Definition-of-done gates (required for each slice)

Every slice is complete only when all gates pass:

1. Code implemented and reviewed
2. Unit tests pass
3. Endpoint integration checks pass
4. `dotnet build` and `dotnet test` are green
5. Security checks pass
6. OpenAPI is updated for behavioral changes
7. Documentation is updated

## Milestone 2 verification status

Authentication and identity flows are now verified end-to-end in the codebase with quality gates:

1. Registration, login, refresh rotation, and logout revocation are covered by automated tests.
2. JWT claims include `sub`, `organization_id`, and `permission` claims.
3. RBAC checks are enforced on recipe and ingredient endpoints for read/manage permissions.
4. Organization scope is enforced from authenticated claims for recipe and ingredient access.
5. Build/test/security checks are required before handoff.

## Milestone 3 handoff rule

Only after Milestone 2 auth slices pass all gates should development move to Milestone 3 recipe and ingredient workflows, reusing the same slice-and-gate delivery model.

## Milestone 3 kickoff slices (core recipe + ingredient workflows)

1. **Recipe slice**
   - Enforce tenant-scoped access and `recipe.read` / `recipe.manage` permissions on recipe endpoints.
2. **Ingredient slice**
   - Enforce tenant-scoped access and `ingredient.read` / `ingredient.manage` permissions on ingredient endpoints.
3. **Quality gate slice**
   - Keep tests, OpenAPI notes, and security checks aligned with each endpoint behavior change.
