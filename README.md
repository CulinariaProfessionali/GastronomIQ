# GastronomIQ

GastronomIQ is a backend-first platform for culinary operations, implemented as a **.NET 8** clean-architecture solution.

## Authoritative baseline

- **Runtime:** .NET 8
- **API:** ASP.NET Core Minimal APIs
- **Architecture:** API + Application + Domain + Infrastructure projects
- **Primary persistence target:** PostgreSQL (with in-memory service implementations for local/dev paths)
- **Security model:** JWT tokens, organization scoping, permission-based authorization
- **Test stack:** xUnit

## Repository structure

- `/Program.cs` + `/Endpoints` + `/Middleware` + `/Security` + `/Configuration`: API host and HTTP surface
- `/GastronomIQ.Application`: contracts and application-level policies
- `/GastronomIQ.Domain`: domain models and logic
- `/GastronomIQ.Infrastructure`: service implementations (identity, recipes, inventory, reporting, persistence adapters)
- `/GastronomIQ.Application.Tests` and `/GastronomIQ.Domain.Tests`: automated test suites
- `/database`: schema and migrations
- `/docs`: architecture and database documentation

## Getting started

### Prerequisites

- .NET SDK 8.0+

### Run

```bash
dotnet restore /home/runner/work/GastronomIQ/GastronomIQ/GastronomIQ.sln
dotnet run --project /home/runner/work/GastronomIQ/GastronomIQ/GastronomIQ.Api.csproj
```

### Test

```bash
dotnet test /home/runner/work/GastronomIQ/GastronomIQ/GastronomIQ.sln
```

## Milestone direction

- **Milestone 2 (current): Authentication & Identity**
  - Login, registration, refresh tokens, logout/revocation
  - RBAC permission coverage
  - Organization scoping and security hardening
  - Unit + endpoint integration checkpoints
- **Milestone 3 (next): Core recipe + ingredient workflows**
  - Execute with the same vertical-slice and quality-gate approach as Milestone 2

For detailed Milestone 2 execution slices, ownership model, and definition-of-done gates, see:

- `/home/runner/work/GastronomIQ/GastronomIQ/docs/MILESTONE_2_AUTH_EXECUTION.md`
