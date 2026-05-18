# Configuration reference

All knobs live in `appsettings.json` (or environment-specific overrides like
`appsettings.Production.json`, or environment variables prefixed `Mcg__` /
`Bridge__`).

## `ConnectionStrings:Default`

SQL Server connection string for the bridge's `IntegrationTransaction` /
`IntegrationAudit` tables. Default uses LocalDB for development.

```
Server=(localdb)\MSSQLLocalDB;Database=MCGCareWEBQI_Dev;Trusted_Connection=True;TrustServerCertificate=True;
```

For production, point this at your SQL Server and apply
[`db/schema.sql`](../db/schema.sql) once.

---

## `Mcg` section — what changes when you swap real MCG in

| Key | Maps to Dev Guide | Description |
|---|---|---|
| `InterfaceLoginUrl` | §4 (entry point)          | Full URL to MCG's `interfacelogin.aspx`. Swap from mock (`http://localhost:7080/…`) to real (`https://<tenant>.carewebqi.com/…`). |
| `WebServicesUrl`    | §6 (Reconcile WS)         | Full URL to MCG's `Reconcile.asmx`. Bridge calls `AcknowledgeMessageByEpisode` after every successful return. |
| `LoginKey`          | §2 (Interface Key)        | The shared interface key. Used as hash salt. **Never commit a real value — use a secret store.** |
| `HashAlgorithm`     | §1.2 step 3, §2.2         | `SHA256` (default) or `SHA512`. API integrations MUST be 256+. `SHA1` and `MD5` exist for legacy support but should not be used. |
| `RequestVersion`    | §4.2 (`requestVersion`)   | API version sent on every request. `12.0` by default. |
| `IsInteractive`     | §3.1 (`isInteractive`)    | `true` (default, recommended) or `false`. |
| `DefaultRequestType`| §4.2 (`requestType`)      | `documentation` (default), `episodesummary`, `guideline`, or `discharge`. Caller can override per-launch. |
| `GuidelinePublicationCodes` | §4.6              | Comma-delimited: `AC,ISC,GRG,MCM,RFC,HC,CCG,TC,BHG,PIM,MCR`. Subset of your licensed products. |
| `ResultTransform`   | §5.1 (XSL transforms)     | Empty = raw XML. Otherwise: `EpisodeSummaryHtml.xslt`, `EpisodeSummaryText.xslt`, `EpisodeSummaryXML.xslt`, `EpisodeSummaryXMLWithCDATA.xslt`. |
| `InterfaceResponseType` | §3.2                  | `RedirectOnly` (recommended) or `ScriptedForm`. The bridge handles both. |

---

## `Bridge` section — bridge-side behavior

| Key | Description |
|---|---|
| `PublicBaseUrl`         | The base URL the bridge will tell MCG to redirect to. Must match how callers reach the bridge from the internet. The full return URL is `{PublicBaseUrl}{ReceiverPath}`. |
| `ReceiverPath`          | Default `/receive`. The path where MCG posts the `CwqiMessage` back. |
| `AutoCloseOnComplete`   | `true` (default) — popup auto-closes after delivering the result. Set `false` to keep the result page visible for debugging. |
| `AllowedCallerOrigins`  | Comma-delimited list of origins allowed to receive `window.postMessage`. `*` for dev only. In production, list each caller origin explicitly (e.g. `https://evokeconnect.example.com,https://other-caller.example.com`). |
| `EnableServerCallback`  | `true` (default) — fire webhook POST when caller supplied `callbackUrl` on launch. |
| `CallbackRetryCount`    | `3` (default) — max attempts before giving up. Backs off `attempt * 2` seconds. |

---

## Serilog

Logs go to console and rolling file `logs/bridge-{date}.log` (or `mockserver-{date}.log`).
Tune log level in `Logging:LogLevel:Default` or via `Serilog:MinimumLevel` if you
prefer Serilog's native config.

---

## Mock server config (`src/MCGCareWEBQI.MockServer/appsettings.json`)

Only one knob matters: `Mcg:LoginKey`. This MUST match the bridge's `Mcg:LoginKey`
or the mock will reject the bridge's POST with HTTP 401 (hash mismatch).

Default in both: `dev-mock-key-CHANGE-ME`.

---

## Environment-variable overrides

ASP.NET Core honors environment variables prefixed `Mcg__` and `Bridge__` (double
underscore = section separator). Useful for CI / containers:

```bash
export Mcg__InterfaceLoginUrl=https://prod.carewebqi.com/interface/interfacelogin.aspx
export Mcg__LoginKey=$(vault read -field=key secret/mcg/prod)
export Bridge__PublicBaseUrl=https://mcg-bridge.example.com
export ConnectionStrings__Default="Server=sql.example.com;Database=McgBridge;..."
```

The bridge picks them up at startup; no `appsettings.json` changes needed.
