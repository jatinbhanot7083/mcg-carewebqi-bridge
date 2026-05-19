# Production deployment with real MCG CareWebQI

This document is the **swap procedure** when you receive MCG interface keys and want
to point the bridge at the real CareWebQI tenant. **No code change required.**

---

## Why the bridge proxies MCG

The dock-in-panel / focus-mode / pop-out features rely on the iframe and the parent
window being **same-origin**. Browsers block cross-origin scripts from reading
`iframe.contentWindow.location`, posting messages back through a frame hierarchy,
or sharing cookies — all of which the state-preservation features depend on.

The bridge solves this in production the same way it solves it in dev: by serving
MCG **through its own domain** via a reverse proxy.

```
                  https://mcg-bridge.your-org.com
                              │
                              ▼
                  ┌───────────────────────┐
                  │  Bridge (this app)    │
                  │  + YARP reverse proxy │── /__mcg/* ──▶ https://tenant.carewebqi.com
                  └───────────────────────┘
                              │
                              ▼
                          SQL Server
```

The browser only sees `mcg-bridge.your-org.com`. The iframe, popup, postMessage
between them, and all the dock/popup/focus transitions work identically in
production to how they work against the mock.

---

## The actual swap (3 settings)

### 1. Connection string

```jsonc
"ConnectionStrings": {
  "Default": "Server=<your-prod-sql>;Database=MCGBridge;User Id=...;Password=...;TrustServerCertificate=True;"
}
```

Run [`db/schema.sql`](../db/schema.sql) once against the database to create the tables.

### 2. MCG settings

```jsonc
"Mcg": {
  // The reverse-proxy upstream. Bridge will forward /__mcg/* to this host.
  "UpstreamHost":      "https://tenant.carewebqi.com",

  // These two stay on the bridge's own domain — they go through the proxy.
  "InterfaceLoginUrl": "https://mcg-bridge.your-org.com/__mcg/interface/interfacelogin.aspx",
  "WebServicesUrl":    "https://mcg-bridge.your-org.com/__mcg/WebServices/Reconcile.asmx",

  // Move LoginKey into a secret store. NEVER commit the real value.
  "LoginKey":          "<from secret store>",

  "HashAlgorithm":     "SHA256",
  "RequestVersion":    "12.0"
}
```

Recommend `LoginKey` via env var:

```bash
export Mcg__LoginKey='<your-real-interface-key>'
```

Or via ASP.NET Core user secrets / Azure Key Vault / HashiCorp Vault / AWS Secrets Manager.

### 3. Bridge settings

```jsonc
"Bridge": {
  "PublicBaseUrl":         "https://mcg-bridge.your-org.com",
  "ReceiverPath":          "/receive",
  "AutoCloseOnComplete":   true,
  "AllowedCallerOrigins":  "https://evokeconnect.your-org.com",  // not '*' in prod
  "EnableServerCallback":  true,
  "CallbackRetryCount":    3
}
```

---

## What the proxy strips from MCG's responses

Real MCG (and most SaaS apps) send response headers that prevent iframe embedding.
The bridge strips these at the proxy layer so the dock-in-panel iframe works:

| Header | Reason for stripping |
|---|---|
| `X-Frame-Options` | Would prevent iframe embedding entirely (`SAMEORIGIN` / `DENY`). |
| `Content-Security-Policy` (the whole header) | The `frame-ancestors` directive can block embedding even when `X-Frame-Options` isn't set. |
| `Content-Security-Policy-Report-Only` | Same as above but report-only mode. |
| `Cross-Origin-Opener-Policy` | Would break the `window.opener` link required for popup → parent postMessage. |
| `Cross-Origin-Embedder-Policy` | Restricts cross-origin resource loading inside the iframe. |

See [`Program.cs`](../src/MCGCareWEBQI.Bridge/Program.cs) and
[`RemoveResponseHeaderTransform.cs`](../src/MCGCareWEBQI.Bridge/Services/RemoveResponseHeaderTransform.cs).

---

## What you still need to test against the real tenant

Things that can only be validated against your actual MCG instance:

- [ ] MCG accepts the bridge's signed POST and renders the clinician UI
- [ ] MCG's session cookies survive the proxy round-trip
  - If not: add a `Set-Cookie` rewrite transform (rewrite `Domain=tenant.carewebqi.com` → `Domain=mcg-bridge.your-org.com` or remove the Domain attribute)
- [ ] MCG's internal navigation (clicking around guidelines) stays inside `/__mcg/*`
  - If not: MCG is using absolute URLs we need to rewrite, or the X-Forwarded-Prefix scheme MCG expects is different
- [ ] MCG's Exit Episode posts back to the bridge's `/receive` endpoint
- [ ] `Reconcile.asmx` SOAP ACK returns success
- [ ] Dock ↔ Pop out ↔ Focus state transitions preserve clinician selections

If any of these break, the proxy can be patched without redeploying any of the
business logic — only adjust `Program.cs` transform configuration.

---

## What MCG might still break — and the fallbacks

Two things a reverse proxy genuinely cannot fix:

1. **MCG's pages run frame-busting JavaScript** (`if (window.top !== window) window.top.location = …`).
   This is rare in modern SaaS but possible. Mitigation: ask MCG / HealthEdge to disable for the bridge domain.

2. **MCG sets Strict-Transport-Security with includeSubDomains** that doesn't match
   the bridge's domain.  Won't break embedding but worth knowing.

If MCG ever proves un-embeddable in production, the **fallback is popup-only mode**.
Set `Bridge:DockModeAvailable: false` (config to add) and the demo caller would hide
the Dock toggle, leaving Pop out as the only mode. Dock-back-with-state would
not be available but the core integration still works end-to-end.

---

## Bandwidth note

Because the bridge proxies every request from clinician → MCG, the bridge needs
network bandwidth proportional to MCG usage. For typical UM workloads (a few hundred
MCG sessions per day) this is negligible. For very high-volume use cases (thousands
of concurrent clinicians) horizontal-scale the bridge behind a load balancer; YARP is
stateless so this is a flat configuration change.
