# MCGCareWEBQI Interface — .NET 8 rebuild

A modernized, application-agnostic bridge between any third-party system and
**MCG CareWebQI 12.0**. Originally written in 2010 on ASP.NET Web Forms +
EntitySpaces; rewritten on .NET 8, ASP.NET Core (Blazor Server + minimal API),
Entity Framework Core, CoreWCF, and MudBlazor.

> **Design philosophy.** The bridge has **zero compile-time coupling** to any
> calling application. Callers integrate by opening a documented launch URL
> in a popup window. Results come back via `window.postMessage`,
> server-to-server webhook, or REST polling — caller picks. Add a new caller
> tomorrow → same URL pattern, zero bridge code change.

## Projects

| Project | Purpose |
|---|---|
| [`src/MCGCareWEBQI.Shared`](src/MCGCareWEBQI.Shared) | Hash helper, CwqiMessage POCOs, config models, request builders. No I/O. |
| [`src/MCGCareWEBQI.Data`](src/MCGCareWEBQI.Data) | EF Core 8 DbContext + entities (`IntegrationTransaction`, `IntegrationAudit`). |
| [`src/MCGCareWEBQI.Bridge`](src/MCGCareWEBQI.Bridge) | The bridge itself. Blazor Server + minimal API. Replaces the old Web Forms project. |
| [`src/MCGCareWEBQI.MockServer`](src/MCGCareWEBQI.MockServer) | Stand-in for real MCG CareWebQI. Hosts `interfacelogin.aspx` + CoreWCF `Reconcile.asmx`. Swap with real MCG via config. |
| [`tests/MCGCareWEBQI.Tests`](tests/MCGCareWEBQI.Tests) | xUnit unit tests (12 tests, all passing). |

## Quick start

```powershell
# 1. Restore + build
dotnet build

# 2. Create LocalDB and apply schema
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF DB_ID('MCGCareWEBQI_Dev') IS NULL CREATE DATABASE MCGCareWEBQI_Dev"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MCGCareWEBQI_Dev -i db\schema.sql

# 3. Run tests
dotnet test

# 4. Start the mock MCG server (one terminal)
dotnet run --project src/MCGCareWEBQI.MockServer        # http://localhost:7080

# 5. Start the bridge (second terminal)
dotnet run --project src/MCGCareWEBQI.Bridge            # http://localhost:7090

# 6. Open the demo caller in your browser
#    http://localhost:7090/demo
#    Fill the form, click "Launch MCG popup", click through, watch the result come back.
```

## End-to-end flow

```
┌──────────────┐                            ┌──────────────────┐
│ EvokeConnect │ ── window.open(launch) ───▶│  Bridge (popup)  │──signed POST──▶ MCG (or Mock)
│  or any      │ ◀── postMessage(result) ───┤                  │ ◀──CwqiMessage──
│  caller      │ ◀── webhook (optional) ────┤                  │──SOAP ACK──▶ Reconcile.asmx
└──────────────┘ ◀── REST poll (optional) ──┤                  │
                                            └──────────────────┘
                                                     │
                                                     ▼
                                              IntegrationTransaction
                                              + IntegrationAudit
```

1. **Caller opens** `/launch?callerId=…&callerTxnId=…&patientId=…` in a popup.
2. **Bridge** binds query string to `LaunchRequest`, writes an `IntegrationTransaction` row,
   builds the signed MCG POST per Dev Guide §4, renders an auto-submitting form.
3. **MCG (or Mock)** validates the hash, opens the clinician documentation UI.
4. **Clinician** documents criteria, adds notes, clicks **Exit Episode**.
5. **MCG** posts `<CwqiMessage>` XML back to bridge's `/receive` endpoint (matches Dev Guide §5).
6. **Bridge** parses the XML, persists, calls Reconcile.asmx to ACK (Dev Guide §6).
7. **Bridge popup** renders a "Done" page that:
   - sends `window.postMessage({ source: 'mcg-bridge', payload: result }, callerOrigin)`
   - auto-closes after ~1.5s
   - (in parallel) POSTs result JSON to caller's `callbackUrl` if supplied
8. **Caller** receives the result via the message listener it registered, or polls
   `GET /api/transactions/{id}` for status.

## Swapping the mock for real MCG

When you receive MCG keys, change **three values** in
`src/MCGCareWEBQI.Bridge/appsettings.json`. No code change.

```jsonc
"Mcg": {
  "InterfaceLoginUrl": "https://<your-tenant>.carewebqi.com/interface/interfacelogin.aspx",
  "WebServicesUrl":    "https://<your-tenant>.carewebqi.com/WebServices/Reconcile.asmx",
  "LoginKey":          "<your-interface-key>"
}
```

The mock can stop running. The bridge will sign every POST with your interface key
and submit to the real CareWebQI tenant. Everything else — request payload format,
hash algorithm, CwqiMessage shape, Reconcile SOAP contract — already conforms to
the CareWebQI 12.0 Developer's Guide.

## Documentation

### For the engineering team taking over

| File | What it covers |
|---|---|
| [docs/HANDOVER-RUN-SAMPLE.md](docs/HANDOVER-RUN-SAMPLE.md) | **Start here.** 10-minute setup to get the sample running locally end-to-end. |
| [docs/HANDOVER-ANGULAR-INTEGRATION.md](docs/HANDOVER-ANGULAR-INTEGRATION.md) | **Read second.** Full integration guide with TypeScript / Angular code for EvokeConnect. |
| [docs/INTEGRATION.md](docs/INTEGRATION.md) | Caller-agnostic HTTP contract (launch URL, result JSON, delivery modes). |
| [docs/CONFIG.md](docs/CONFIG.md) | Every `appsettings.json` key, mapped to its Dev Guide section. |
| [docs/PRODUCTION-DEPLOYMENT.md](docs/PRODUCTION-DEPLOYMENT.md) | Swap procedure when real MCG keys + tenant URL arrive. |

### For MCG certification

| File | What it covers |
|---|---|
| [docs/CERT-PREREQ.md](docs/CERT-PREREQ.md) | MCG-tenant Application Settings + bridge config cross-reference. |
| [docs/CERT-CHECKLIST.md](docs/CERT-CHECKLIST.md) | Every Appendix A cert step mapped to our solution. |
| [docs/CERT-DRY-RUN-SCRIPT.md](docs/CERT-DRY-RUN-SCRIPT.md) | Step-by-step internal walkthrough using the mock. |
| [docs/CERT-FORMAL-SIGN-OFF.md](docs/CERT-FORMAL-SIGN-OFF.md) | Printable Pass/Fail form for the MCG cert reviewer. |
| [docs/CERTIFICATION-CHECKLIST.md](docs/CERTIFICATION-CHECKLIST.md) | Legacy Dev Guide Appendix A self-check (kept for completeness). |

## What's intentionally NOT here

- **Caller-specific business-object DLLs.** Original code linked to compiled
  EntitySpaces-based DLLs from the prior host application. The new design doesn't
  write into any caller's tables — it owns its own `IntegrationTransaction` table only.
- **EntitySpaces.** Replaced with EF Core 8 / LINQ.
- **ASP.NET Web Forms.** Replaced with Blazor Server + minimal API.
- **Pixel-perfect MCG UI.** Mock UI is functional and labeled correctly. When you
  send screenshots, we can match the layout.

## Stack

- .NET 8 (`net8.0`) — SDK pinned via `global.json` (`rollForward: latestFeature`)
- ASP.NET Core 8, Blazor Server (interactive)
- Entity Framework Core 8 + SQL Server (LocalDB for dev)
- CoreWCF 1.x for SOAP server (`Reconcile.asmx`)
- MudBlazor 8.0 for UI components
- Serilog for logging (console + rolling file in `logs/`)
- xUnit for tests
