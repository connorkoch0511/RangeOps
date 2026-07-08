# legacy (Classic ASP)

The **legacy reports** for RangeOps — small **Classic ASP (VBScript)** pages that
read the shared database over **ADO/ODBC** and render plain HTML reports. They're
the "before" in a modernization story: the **Django web dashboard replaced
these**, but they're kept as a working system-of-record report during migration —
exactly the kind of *maintenance and sustainment of legacy systems* the work
involves.

| File | What it is |
|------|-----------|
| `schedule-report.asp` | Mission Schedule report — legacy equivalent of the modern schedule board |
| `mission-report.asp?id=<n>` | Per-mission test-run report — legacy equivalent of the mission-detail page (uses a **parameterized** ADO command) |
| `lib.asp` | Shared connection + HTML header/footer helpers (`#include`d) |
| `config.example.asp` | Connection-string template → copy to `config.asp` (git-ignored) |

## What it demonstrates

- **Classic ASP / VBScript** — `<% %>` scripting, `Server.CreateObject`, `Option Explicit`
- **ADO** — `ADODB.Connection`, `ADODB.Command`, `ADODB.Recordset`
- **SQL** against the shared RangeOps schema (the same tables the modern stack uses)
- **Security-minded legacy code** — parameterized queries (no string-concatenated
  SQL), `IsNumeric` input validation, and `Server.HTMLEncode` output encoding

## Running it

Classic ASP runs on **Windows + IIS** (it can't run on Linux/Vercel), so this is
a Windows-host artifact:

1. Install the **PostgreSQL ODBC driver** (psqlODBC) on the IIS host.
2. Enable the **ASP** role feature in IIS and add this folder as an application.
3. Copy `config.example.asp` → `config.asp` and set the connection string
   (defaults to the local docker-compose Postgres on `:5544`).
4. Browse to `schedule-report.asp`.

## The modernization story

These reports were read-only HTML over ADO/ODBC. The
[Django dashboard](../dashboard/) is their modern replacement — same database,
same reports, but with a maintainable template/ORM stack, tests, and CI. Being
able to keep the legacy report running *and* stand up the modern one against the
same schema is the sustainment pattern this mirrors.
