# api (C# · ASP.NET Core · EF Core)

A small **REST API** over the shared RangeOps database, built with **ASP.NET
Core 8** (minimal APIs) and **EF Core** — reusing `RangeOps.Core`'s entities and
`DbContext`.

It's the **production-correct data layer**: instead of every client connecting
straight to the database, they'd call this API, which holds the credentials
server-side and returns DTOs (never the raw entities). It also demonstrates REST
API design, request validation, and OpenAPI.

## Endpoints

| Method | Route | Returns |
|--------|-------|---------|
| GET | `/health` | liveness check |
| GET | `/api/missions` | all missions with test-run counts |
| GET | `/api/missions/{id}` | a mission and its test runs |
| GET | `/api/runs/{id}/telemetry` | telemetry summary (samples, link dropouts, max alt/IAS) |
| POST | `/api/missions` | create a mission (validated) |

Interactive docs at **`/swagger`** when running.

## Run

```bash
docker compose up -d db            # shared database (repo root)
POSTGRES_HOST=localhost POSTGRES_PORT=5544 \
  dotnet run --project api/RangeOps.Api    # http://localhost:5099 (or as configured)
```

It resolves the database the same way the rest of the stack does (`POSTGRES_*`
env → build default → local docker), so no separate configuration.

## Tests

```bash
dotnet test console/RangeOps.sln    # includes RangeOps.Api.Tests
```

`RangeOps.Api.Tests` boots the API in-memory with `WebApplicationFactory` and
exercises the endpoints over HTTP against a real Postgres (health, mission
create/get round-trip, validation, telemetry summary).

## Note

ASP.NET Core isn't Vercel-native (Vercel doesn't run .NET), so this API is a
runnable/tested component rather than a deployed one — it would live on a
container host (Render, Fly.io, Azure) in production.
