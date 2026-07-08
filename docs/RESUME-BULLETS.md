# RangeOps — résumé bullets

Drop-in bullets for the JT4 / flight-test software application. Pick 3–5.

## Concise (3 bullets)

- Built a multi-language flight-test **range operations suite** (C, C#/.NET 8,
  Python/Django, Classic ASP) that automates mission scheduling and telemetry
  capture, integrating a desktop operator console, a C telemetry source, and a
  web dashboard through **one shared PostgreSQL schema via two ORMs** (EF Core +
  Django ORM).
- Shipped a cross-platform **C#/.NET 8 desktop console** (Avalonia, XAML/MVVM)
  that schedules test missions and captures/validates live telemetry via EF Core,
  published as self-contained **macOS/Windows/Linux** builds through a GitHub
  Actions **release pipeline**.
- Established **unit, integration, system, and end-to-end (Playwright) tests** in
  CI on every push, plus a scripted pipeline test asserting fault detection across
  all components.

## Fuller (add as needed)

- Wrote a **C telemetry source** (POSIX sockets) streaming 5 Hz aircraft telemetry
  with injectable **data-link dropouts**; the console and dashboard detect and
  flag the dropouts end to end.
- Deployed the **Django dashboard on Vercel** with managed PostgreSQL; designed
  **least-privilege database access** and build-time secret injection so
  distributed clients share the live database **without shipping credentials in
  source**.
- Built a **REST API** (ASP.NET Core 8 minimal APIs + EF Core) over the shared
  schema with DTOs, request validation, OpenAPI/Swagger, and
  `WebApplicationFactory` integration tests — the production-correct data layer.
- Maintained **legacy Classic ASP (VBScript/ADO)** reports and modernized them
  into the Django dashboard against the same schema — legacy **sustainment and
  migration**.
- Designed a **database-first** integration where the SQL schema is the contract
  between systems (neither ORM owns migrations), mirroring real range integration
  work.

## Interview one-liner

> "RangeOps is a flight-test range operations suite: a C telemetry rig, a C#/.NET
> desktop operator console, and a Python/Django web dashboard, all sharing one
> PostgreSQL database. It automates the scheduling and telemetry-capture side of
> test ops, with the legacy Classic ASP reports it modernized kept alongside."
