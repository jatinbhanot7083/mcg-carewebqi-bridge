# Handover — How to run the sample solution

For the engineering team to get the bridge running locally end-to-end against the
mock MCG server. ~10 minutes from a fresh machine.

## 1. Prerequisites

Install once, machine-wide:

| Tool | Why | Verify |
|---|---|---|
| **.NET 10 SDK** | Builds + runs the solution. | `dotnet --list-sdks` shows `10.0.x` |
| **SQL Server LocalDB** | Bridge persists `IntegrationTransaction` rows here. Comes free with SQL Server Express or Visual Studio. | `sqllocaldb info` shows `MSSQLLocalDB` |
| **Git** | Clone the repo. | `git --version` |
| **Visual Studio 2022 17.x or VS Code** (optional) | For IDE debugging. CLI works fine too. | — |
| **Chrome / Edge** (latest) | Browser to drive the demo. Required for dock/popup/focus features. | — |

The solution targets `net10.0` exclusively. .NET 8 / .NET 9 won't compile it.

## 2. Get the code

```powershell
cd C:\src                                          # or wherever your code lives
git clone <your-repo-url> mcg-bridge
cd mcg-bridge\MCGCareWEBQI_Net8
```

If you've been given a zip instead, extract and `cd` into the `MCGCareWEBQI_Net8` folder.

## 3. Restore + build

```powershell
dotnet restore
dotnet build
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s).` across 5 projects:
- `MCGCareWEBQI.Shared`
- `MCGCareWEBQI.Data`
- `MCGCareWEBQI.Bridge`
- `MCGCareWEBQI.MockServer`
- `MCGCareWEBQI.Tests`

## 4. Create the dev database

```powershell
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF DB_ID('MCGCareWEBQI_Dev') IS NULL CREATE DATABASE MCGCareWEBQI_Dev"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MCGCareWEBQI_Dev -i db\schema.sql
```

Verify tables created:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MCGCareWEBQI_Dev -Q "SELECT name FROM sys.tables ORDER BY name"
```

You should see `IntegrationAudit` and `IntegrationTransaction`.

## 5. Run unit tests

```powershell
dotnet test
```

Expected: `Passed!  - Failed: 0, Passed: 12, Skipped: 0` covering Hash, McgRequestBuilder, and CwqiMessage parsing.

## 6. Run the two servers

You need **two PowerShell windows** open simultaneously:

**Window A — Mock MCG server** (stands in for real MCG until keys arrive):
```powershell
cd C:\src\mcg-bridge\MCGCareWEBQI_Net8
dotnet run --project src\MCGCareWEBQI.MockServer
# Listens on http://localhost:7080
```

**Window B — Bridge** (the actual integration service):
```powershell
cd C:\src\mcg-bridge\MCGCareWEBQI_Net8
dotnet run --project src\MCGCareWEBQI.Bridge
# Listens on http://localhost:7090
```

Both processes stay running. Don't close the windows.

## 7. Smoke test in the browser

Open Chrome to **http://localhost:7090/demo**. You should see the EvokeConnect
Decision Review screen with `Auth 1109SYY0F · Donald W Charleston` at top.

Run the 60-second click-through:

1. Scroll to **Decision Support → External Guidelines** section
2. Pick **Dock** mode (default), click **▶ Launch MCG**
3. MCG search page renders on the right
4. Click **Select** on M-130 Diabetes
5. Click **Care Planning** link
6. Check 2-3 criteria under "Hyperglycemic hyperosmolar state"
7. Click **Save** → **Exit Episode**
8. Result row appears with status **Met**
9. Click **View trace** — confirm all 4 integration trace sections are populated

If all 9 steps work, the solution is running correctly.

## 8. Verify the DB is being written

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d MCGCareWEBQI_Dev -Q "SELECT TOP 5 TransactionId, CallerId, Status, CreatedAt FROM dbo.IntegrationTransaction ORDER BY CreatedAt DESC"
```

You should see one row per launch with `Status='Acknowledged'`.

## 9. Stopping

- `Ctrl+C` in each PowerShell window
- LocalDB stays running in the background (harmless; will idle out)

## 10. Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `dotnet --list-sdks` doesn't show 10.0.x | .NET 10 not installed | Install from <https://dotnet.microsoft.com> |
| `Cannot open server '(localdb)\MSSQLLocalDB'` | LocalDB not running | `sqllocaldb start MSSQLLocalDB` |
| Bridge launch fails with `Login failed for user` | DB was dropped | Re-run step 4 |
| `port 7080 already in use` | Old MockServer still running | `Get-Process MCGCareWEBQI* \| Stop-Process` |
| `dotnet build` errors `Yarp.ReverseProxy not found` | NuGet restore failed | `dotnet restore --force` |
| Iframe loads then says "Sorry, nothing found" | Stale browser cache after code change | Hard-refresh: Ctrl+Shift+R |
| Dock-back doesn't work | Browser blocked popup | Allow popups for `localhost:7090` |

## 11. What to do next

Once running, read these in order:

1. [`docs/HANDOVER-ANGULAR-INTEGRATION.md`](HANDOVER-ANGULAR-INTEGRATION.md) — how to call the bridge from your Angular EvokeConnect app
2. [`docs/INTEGRATION.md`](INTEGRATION.md) — the formal launch URL + result contract (caller-agnostic)
3. [`docs/CONFIG.md`](CONFIG.md) — every `appsettings.json` knob
4. [`docs/PRODUCTION-DEPLOYMENT.md`](PRODUCTION-DEPLOYMENT.md) — swap procedure when real MCG keys arrive
5. [`docs/CERT-CHECKLIST.md`](CERT-CHECKLIST.md) — what's cert-ready vs what's mock-only
