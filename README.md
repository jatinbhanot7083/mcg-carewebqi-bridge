# MCGCareWEBQI Interface — .NET 10 rebuild

A modernized rebuild of the MCG CareWebQI integration originally written in 2010
on ASP.NET Web Forms + EntitySpaces. Now targets .NET 10, ASP.NET Core
(Blazor Server + minimal API), Entity Framework Core, and CoreWCF.

## Projects

| Project | Purpose |
|---|---|
| `src/MCGCareWEBQI.Shared` | Hash helper, CwqiMessage POCOs, config models, request builders. No I/O. |
| `src/MCGCareWEBQI.Data` | EF Core 10 DbContext + entities for `InterfaceLog`, `UmMillimanLog`, `MillimanLog`, etc. |
| `src/MCGCareWEBQI.Bridge` | The bridge between GuidingCare and MCG CareWebQI. Replaces the old Web Forms project. |
| `src/MCGCareWEBQI.MockServer` | Stand-in for real MCG CareWebQI (interfacelogin + Reconcile.asmx). Swap with real MCG via config. |
| `tests/MCGCareWEBQI.Tests` | xUnit integration + unit tests. |

## Quick start

```powershell
# 1. Restore + build
dotnet build

# 2. (Phase 1 onwards) Create LocalDB and seed schema
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE MCGCareWEBQI_Dev"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MCGCareWEBQI_Dev -i db\schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MCGCareWEBQI_Dev -i db\seed.sql

# 3. (Phase 2 onwards) Run mock MCG server
dotnet run --project src/MCGCareWEBQI.MockServer

# 4. (Phase 3 onwards) Run the bridge
dotnet run --project src/MCGCareWEBQI.Bridge
```

## Swapping mock for real MCG

When MCG keys arrive, change three settings in `src/MCGCareWEBQI.Bridge/appsettings.json`:

```json
"Mcg": {
  "InterfaceLoginUrl": "https://<your-tenant>.carewebqi.com/interface/interfacelogin.aspx",
  "WebServicesUrl":   "https://<your-tenant>.carewebqi.com/WebServices/Reconcile.asmx",
  "LoginKey":         "<your-interface-key>"
}
```

No code change required.

## Conformance to MCG CareWebQI 12.0 Developer's Guide

See `docs/CERTIFICATION-CHECKLIST.md` for a section-by-section self-check.
