# Swapping from the mock server to real MCG CareWebQI

This is the procedure to flip from the local stub to a real MCG tenant when you
receive your interface key. It is **config-only**. No code changes are required.

## Prerequisites

You should have received from MCG Technical Support:

- [ ] **Interface Login URL** — usually `https://<your-tenant>.carewebqi.com/interface/interfacelogin.aspx`
- [ ] **Web Services URL** — usually `https://<your-tenant>.carewebqi.com/WebServices/Reconcile.asmx`
- [ ] **Interface Key** — a base64-ish secret string. Treat like a password.
- [ ] **Confirmed hash algorithm** — should be `SHA256` or `SHA512` per Dev Guide §1.2 step 3.
- [ ] **Confirmed `requestVersion`** — usually `12.0`.
- [ ] **Licensed publication codes** for guideline searches (subset of `AC, ISC, GRG, MCM, RFC, HC, CCG, BHG, TC, PIM, MCR`).
- [ ] **Trusted-caller IP** if your tenant has referrer locking enabled (§2.3).
- [ ] **Allowed return URL(s)** — the bridge's public `PublicBaseUrl + ReceiverPath`. MCG must allowlist this.

## Step 1 — Stop the mock server (optional)

The mock server is only for dev. If it's running, stop it. The bridge does not
depend on it once you swap the URLs.

## Step 2 — Move the interface key out of source

Do NOT put the real interface key in `appsettings.json`. Pick one:

**A. Environment variables (simplest for containers / CI):**

```bash
export Mcg__LoginKey='<your-real-key>'
```

**B. ASP.NET Core secrets (dev only):**

```bash
cd src/MCGCareWEBQI.Bridge
dotnet user-secrets init
dotnet user-secrets set "Mcg:LoginKey" "<your-real-key>"
```

**C. Production secret store** — Azure Key Vault, HashiCorp Vault, AWS Secrets
Manager, etc. Wire via the standard ASP.NET Core configuration provider for your
vault.

## Step 3 — Update `appsettings.json` (or env vars) with the real URLs

```jsonc
"Mcg": {
  "InterfaceLoginUrl": "https://<your-tenant>.carewebqi.com/interface/interfacelogin.aspx",
  "WebServicesUrl":    "https://<your-tenant>.carewebqi.com/WebServices/Reconcile.asmx",
  // LoginKey comes from secret store above
  "HashAlgorithm":     "SHA256",
  "RequestVersion":    "12.0",
  "GuidelinePublicationCodes": "<your licensed codes>",
  "InterfaceResponseType":     "RedirectOnly"
},
"Bridge": {
  "PublicBaseUrl":         "https://mcg-bridge.your-org.example",
  "ReceiverPath":          "/receive",
  "AllowedCallerOrigins":  "https://evokeconnect.your-org.example,https://other-caller.example"
}
```

## Step 4 — Verify the bridge can reach MCG

From the bridge's host:

```bash
# Should return MCG's WSDL — confirms network connectivity and DNS.
curl -sSI https://<your-tenant>.carewebqi.com/WebServices/Reconcile.asmx?wsdl

# Should hit MCG's interfacelogin and respond (usually with a redirect to a login error
# page — that's fine; it proves the URL is reachable).
curl -sSI https://<your-tenant>.carewebqi.com/interface/interfacelogin.aspx
```

If either fails, fix firewall / proxy / DNS before continuing.

## Step 5 — Smoke test

Restart the bridge and run the same end-to-end script from a non-prod caller
launch URL. Watch for:

- HTTP 302 from MCG's `interfacelogin.aspx` redirecting to MCG's clinician UI.
  (If you see HTTP 401 or a `<cwqierror code="2001">` envelope about
  "interface key invalid", your `LoginKey` is wrong or your `HashAlgorithm`
  doesn't match what MCG expects.)
- A `<CwqiMessage>` posted back to `/receive` on exit.
- `IntegrationTransaction.Status = 'Acknowledged'` row in your DB.

Common errors and what they mean:

| Symptom | Likely cause |
|---|---|
| 401 from `interfacelogin.aspx` | Wrong `LoginKey` OR wrong `HashAlgorithm` OR field set didn't match what MCG hashed. |
| `<cwqierror code="12012">` | `patientFirstName` (or other required field) missing for new patient. |
| `<cwqierror code="2001">` | Interface key invalid. |
| 403 / referrer error | MCG tenant has referrer locking on; your bridge's egress IP must be allowlisted. |
| Bridge `/receive` never hit | MCG can't reach your `returnUrl`. Make sure `Bridge:PublicBaseUrl` is internet-reachable and MCG has it allowlisted. |
| Reconcile SOAP 404 | `Mcg:WebServicesUrl` typo (e.g. case-sensitive path on real tenants). |

## Step 6 — Rotate the mock key out

Once real-MCG smoke test passes, the mock's `dev-mock-key-CHANGE-ME` value in
`src/MCGCareWEBQI.MockServer/appsettings.json` no longer matters in production —
the mock isn't deployed. Leave it as-is for local development.

## Step 7 — Lock down `AllowedCallerOrigins`

For production, change `Bridge:AllowedCallerOrigins` from `*` to a comma-delimited
list of the actual caller origins. The bridge will only `postMessage` to those.

## Going back to the mock

To revert (for local debugging, regression tests, etc.), just change the three
`Mcg` URLs back to `http://localhost:7080/…` and restart the bridge. The mock
server doesn't need anything else.

## What to NOT do

- Do not commit the real interface key to git.
- Do not enable `Bridge:AutoCloseOnComplete=false` in production except for short
  debug sessions; it will leave popup windows open on every transaction.
- Do not change the bridge's `ReceiverPath` after MCG has allowlisted it without
  coordinating with MCG to update the allowlist.
- Do not run the bridge over HTTP in production — terminate TLS in front of it
  (Kestrel + cert, or NGINX / IIS / Azure Front Door).
