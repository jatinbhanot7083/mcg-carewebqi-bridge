# MCG CareWebQI Certification — Prerequisites

This document maps every MCG-side Application Setting from the *CareWebQI Certification
Script Checklist v10.0* (page 68, "Certification prerequisites") to where the same
behavior lives in this bridge.

When real MCG cert begins, two checklists need to be confirmed:
1. **MCG-tenant settings** that MCG Technical Support configures inside the CareWebQI Admin UI.
2. **Bridge settings** in `appsettings.json` that drive the partner-app side.

---

## 1. MCG-tenant Application Settings

MCG cert team will verify these inside CareWebQI:
`Admin link > Configuration Administrator > Application Setting Administrator`.

These are **not under our control** — they get configured by your MCG account team
before cert begins. Documented here for completeness so you can request them by name.

| Setting | Required value | Why |
|---|---|---|
| `Documentable Definitions` | `True` | Lets clinicians document inside definition popups (cert §3 step 2d). |
| `Mandatory Variance` | `True` | Forces a Variance category/reason when a milestone is not met (cert §3 step 3j). |
| `Use the Simplified Discharge Workflow` | `True` | Enables the streamlined Exit flow used throughout cert. |
| `Enable Notes Deletion` | `On - Delete notes with strikethrough` | Cert §3 steps 9, 10, 12 delete notes; they must survive as strikethrough. |
| `Allow non-consecutive day documentation of recovery course` | `True` | Cert §3 step 11 adds care dates out of sequence. |
| `Interface: Hash Type (SHA1/MD5/SHA256/SHA512)` | `SHA512` (or `SHA256`) | Bridge sends `messageHash` using this algorithm. |
| `Interface: Response Type` | `RedirectOnly` | Cert flow expects redirect-only response posting back to the bridge. |
| `Interface: Allow Patient Merge` | `True` (default is False) | Cert §7 walks the merge prompt — must be enabled. |
| `Open discharged episode workflow` | `Auto-undischarge` | Cert §3 (variance OFF block) re-opens a discharged episode. |

### How to request these from MCG

Email MCG Technical Support before cert with the table above. They'll confirm in
writing that your tenant matches. Keep that email — the cert reviewer will ask.

---

## 2. Bridge-side `appsettings.json` settings

These are under our control. They map 1:1 to the MCG settings on the right.

```jsonc
{
  "ConnectionStrings": {
    "Default": "Server=<prod-sql>;Database=MCGBridge;User Id=...;Password=...;"
  },

  "Mcg": {
    // The reverse-proxy upstream. Bridge forwards /__mcg/* to this host.
    "UpstreamHost":      "https://tenant.carewebqi.com",

    // These two stay on the bridge's domain — they go through the proxy.
    "InterfaceLoginUrl": "https://mcg-bridge.your-org.com/__mcg/interface/interfacelogin.aspx",
    "WebServicesUrl":    "https://mcg-bridge.your-org.com/__mcg/WebServices/Reconcile.asmx",

    // Pulled from your secret store at runtime — NEVER commit.
    "LoginKey":          "<MCG-issued interface key>",

    // ===== These must match the corresponding MCG-tenant setting =====
    "HashAlgorithm":         "SHA256",        // → MCG "Interface: Hash Type"
    "InterfaceResponseType": "RedirectOnly",  // → MCG "Interface: Response Type"
    "AllowPatientMerge":     true,            // → MCG "Interface: Allow Patient Merge"

    "RequestVersion":            "12.0",
    "DefaultRequestType":        "documentation",
    "IsInteractive":             true,
    "GuidelinePublicationCodes": "AC,ISC,GRG,MCM,RFC,HC,CCG,BHG,TC,PIM,MCR",
    "ResultTransform":           ""
  },

  "Bridge": {
    "PublicBaseUrl":         "https://mcg-bridge.your-org.com",
    "ReceiverPath":          "/receive",
    "AutoCloseOnComplete":   true,
    "AllowedCallerOrigins":  "https://evokeconnect.your-org.com",  // explicit, not '*'
    "EnableServerCallback":  true,
    "CallbackRetryCount":    3
  }
}
```

### Verification sequence at cert time

1. `dotnet build` → 0 warnings, 0 errors.
2. `dotnet test` → all unit tests green (Hash algorithm, request builder, CwqiMessage / CwqiError parsers).
3. Apply [`db/schema.sql`](../db/schema.sql) against production SQL Server.
4. Verify [`docs/PRODUCTION-DEPLOYMENT.md`](PRODUCTION-DEPLOYMENT.md) checklist is complete.
5. Run the [`CERT-DRY-RUN-SCRIPT.md`](CERT-DRY-RUN-SCRIPT.md) end-to-end against the
   mock first, then again against the real MCG tenant.
6. Have the cert reviewer fill in [`CERT-FORMAL-SIGN-OFF.md`](CERT-FORMAL-SIGN-OFF.md).

---

## 3. What MCG cert will NOT ask the bridge to do

For honesty, items in the cert script that are purely MCG-UI features and don't
involve the bridge at all:

- Milestone selection, "Save & Next" navigation, Add Care Date pickers
- Variance category/reason dropdown content
- Calculator inputs and outputs
- Strikethrough rendering of deleted notes
- Search-results sort order *inside* MCG
- Episode summary template content
- Customized guideline (GMM module) authoring

These are tested on MCG's side. The bridge's job is to launch correctly, receive
the returned `CwqiMessage`, persist the result, and acknowledge via Reconcile.asmx
— all of which is covered by the cert items in `CERT-CHECKLIST.md`.
