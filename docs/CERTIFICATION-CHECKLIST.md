# CareWebQI 12.0 Certification self-check

Self-check against Dev Guide Appendix A. Items marked ✅ are implemented and
verifiable from code; ⚠️ are partially implemented; ❌ require a real MCG
environment to validate.

| # | Item | Status | Notes / file |
|---|---|---|---|
| 1 | Use SHA-256 or SHA-512 for hash calculation (§1.2 step 3) | ✅ | Default `SHA256`; SHA-512 supported. [`CwqiHash.cs`](../src/MCGCareWEBQI.Shared/Hashing/CwqiHash.cs). MD5/SHA1 exist for legacy but not recommended. |
| 2 | Order request key/value pairs alphabetically by key before hashing (§2.2) | ✅ | `CwqiHash.CanonicalizeFields` uses `StringComparer.Ordinal` ascending. |
| 3 | HTML-encode field values (UTF-8) before joining (§2.2) | ✅ | `WebUtility.HtmlEncode` in `CanonicalizeFields`. |
| 4 | Append the interface key as the hash salt (§2.2) | ✅ | `Compute(plaintext + key)`. Key never appears as a field. |
| 5 | Send `messageHash` parameter with the base64-encoded hash (§2.2) | ✅ | `McgRequestBuilder.Build` appends `messageHash` last. |
| 6 | Submit all requests to `interfacelogin.aspx` (§4 entry point) | ✅ | Bridge POSTs to `Mcg:InterfaceLoginUrl`. |
| 7 | Differentiate request types by `requestType` value (§4) | ✅ | `documentation` / `episodesummary` / `guideline` / `discharge` supported. |
| 8 | `documentingUser` is required on every request (§4.2) | ✅ | `McgRequestBuilder` defaults to `"Api-User"` if caller does not supply. |
| 9 | Send `returnUrl` for redirect/post-back (§4.2) | ✅ | Built from `Bridge:PublicBaseUrl + Bridge:ReceiverPath`. |
| 10 | Recommend `isInteractive=True` (§3.1 best practice) | ✅ | Default `true`. |
| 11 | Recommend `RedirectOnly` response type (§3.2 best practice) | ✅ | Default; `ScriptedForm` also handled. |
| 12 | Parse `<CwqiMessage>` response XML (§5.2) | ✅ | [`CwqiMessage.cs`](../src/MCGCareWEBQI.Shared/Models/Mcg/CwqiMessage.cs). |
| 13 | Parse `<cwqierror>` envelope and propagate error codes (§7.1) | ✅ | [`CwqiError.cs`](../src/MCGCareWEBQI.Shared/Models/Mcg/CwqiError.cs); included in result payload. |
| 14 | Call Reconcile WS to ACK completed episodes (§6) | ✅ | [`ReconcileSoapClient.cs`](../src/MCGCareWEBQI.Bridge/Services/ReconcileSoapClient.cs); namespace `http://www.carewebqi.com/WS/Reconcile`. |
| 15 | Log every interface request for audit (§7.2 — Interface Activity Log equivalent) | ✅ | `IntegrationTransaction` + `IntegrationAudit` tables capture every state transition. |
| 16 | Support all four request types | ✅ | `documentation`, `episodesummary`, `guideline`, `discharge`. |
| 17 | Pass `episodeCodes` in the documented `CodeNum\|CodeType\|Description\|presentOnAdmission$…` format (§4.3) | ⚠️ | Pass-through — caller is responsible for formatting per Dev Guide. Validate in your caller. |
| 18 | UTC-based `CacheTimeStamp` within 5 minutes of MCG server time (§4.2, optional feature) | ⚠️ | Not auto-attached. Add `CacheTimeStamp` to `McgRequestBuilder` if your tenant enables hash expiration. |
| 19 | XSL transforms applied when configured (§5.1) | ✅ | `Mcg:ResultTransform` passed through as `resultTransform`. |
| 20 | Honor branding requirements when displaying MCG content (§8) | ❌ | Caller's UI responsibility. Bridge does not render MCG clinical content directly. |
| 21 | Storage rules for MCG content (§8.2) | ⚠️ | Raw `McgResponseXml` and `McgResponseJson` are stored in the bridge DB. Review §8.2 retention policy for your contract. |
| 22 | End-user license display (§8.6) | ❌ | Caller's UI responsibility. |
| 23 | Browser compatibility — Chrome / Edge in native mode (§11.1) | ✅ | Blazor Server runs in any modern evergreen browser. |
| 24 | Trusted-caller referrer / IP allowlist (§2.3) | ⚠️ | Currently uses hash-only validation (matches mock & default real-MCG config). If your MCG enables referrer locking, add Firewall / WAF rules in front of the bridge — the bridge itself does not enforce. |

## Things that need MCG itself to certify

- Sample Client utility (§9) compatibility — irrelevant; the bridge replaces the sample client.
- Live integration testing with your tenant — needs keys.
- Final Appendix A walkthrough with MCG Technical Support.

## Suggested pre-certification checklist for your team

- [ ] Replace `Mcg:LoginKey` with a real value from a secret store.
- [ ] Set `Bridge:AllowedCallerOrigins` to a comma-delimited list of caller origins (not `*`).
- [ ] Set `Bridge:PublicBaseUrl` to your bridge's public URL (HTTPS required by MCG).
- [ ] Apply `db/schema.sql` to your production SQL Server.
- [ ] Verify the bridge serves HTTPS in front of a reverse proxy (Kestrel handles HTTPS natively, or terminate at NGINX / IIS / Azure Front Door).
- [ ] Run [`tests/MCGCareWEBQI.Tests`](../tests/MCGCareWEBQI.Tests) in your CI.
- [ ] Test launch from each calling application in a staging tenant.
