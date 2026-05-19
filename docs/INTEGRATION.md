# Integrating a calling application with the MCG Bridge

This document is for engineers building a **caller** (e.g. EvokeConnect or any other
host system). It describes the public contract: how to launch a documentation session
and how to receive the result.

The contract is intentionally HTTP + query string + JSON. No SDK, no DLL.

---

## 1. Launching a session

Open the bridge's `/launch` URL in a popup window:

```javascript
const launchUrl = new URL('https://mcg-bridge.example.com/launch');
launchUrl.searchParams.set('callerId',         'evokeconnect');
launchUrl.searchParams.set('callerTxnId',      crypto.randomUUID());
launchUrl.searchParams.set('patientId',        'P-12345');
launchUrl.searchParams.set('patientFirstName', 'Jane');
launchUrl.searchParams.set('patientLastName',  'Doe');
launchUrl.searchParams.set('patientDateOfBirth', '01/15/1980');
launchUrl.searchParams.set('patientGender',    'Female');
launchUrl.searchParams.set('episodeType',      'Inpatient');
launchUrl.searchParams.set('episodeAdmitDate', '05/18/2026');
launchUrl.searchParams.set('episodeCodes',     'I10|icd10cm');
// Optional:
launchUrl.searchParams.set('callbackUrl',      'https://evokeconnect.example.com/api/mcg/callback');
launchUrl.searchParams.set('returnContext',    JSON.stringify({ uiRoute: '/cases/789' }));

window.open(launchUrl.toString(), 'mcg-popup', 'width=1200,height=820');
```

### Required parameters

| Field | Description |
|---|---|
| `callerId`    | Identifies your application (free-form, used for logging + origin allowlist). |
| `callerTxnId` | Your own correlation id — echoed back on every result so you can match. |

### Optional MCG payload

Every Dev Guide §4 documentation parameter is accepted with the **same name** —
no remapping. The bridge will pass them through to MCG verbatim.

Common ones: `episodeID`, `episodeType`, `episodeAdmitDate`, `episodeCodes`,
`patientID`, `patientFirstName`, `patientLastName`, `patientDateOfBirth`,
`patientGender`, `facilityID`, `facilityName`, `attendingProviderID`, …
See [`LaunchRequest.cs`](../src/MCGCareWEBQI.Shared/Models/Launch/LaunchRequest.cs)
for the full list.

### Optional bridge-only parameters

| Field | Description |
|---|---|
| `callbackUrl`   | If set, the bridge POSTs the result JSON to this URL when MCG returns. |
| `returnContext` | Any string the bridge echoes back in the result unchanged. Useful for caller-side routing/state. |
| `requestType`   | `documentation` (default), `episodesummary`, `guideline`, or `discharge`. |

---

## 2. Receiving the result

Pick **one** (or all) of three delivery modes.

### Mode A — `window.postMessage` (recommended for browser callers)

The bridge popup sends a message to `window.opener` on completion, then closes.
Register a listener once, anywhere in your SPA:

```javascript
window.addEventListener('message', (event) => {
  if (event.data?.source !== 'mcg-bridge') return;
  const result = event.data.payload;  // see "Result shape" below
  console.log('MCG returned', result.status, result.mcgResponse?.episodeId);
  // Update your UI here.
});
```

For production, set `Bridge:AllowedCallerOrigins` to a comma-delimited list of
your caller origins (instead of `*`) — the bridge will only post to those.

### Mode B — Server callback (webhook)

Supply `callbackUrl=https://your-host/api/mcg/callback` on the launch URL.
The bridge will POST the result JSON to that URL with `Content-Type: application/json`,
retrying up to `Bridge:CallbackRetryCount` times on non-2xx responses.

Your handler should be idempotent — the same `transactionId` may arrive more than once.

### Mode C — REST poll

If you have no browser opener and no public callback URL, poll:

```
GET https://mcg-bridge.example.com/api/transactions/{transactionId}
```

`transactionId` is the bridge-assigned GUID. You won't have it directly from the
launch URL (the bridge assigns it after the launch). Two ways to get it:

1. **Read it from `event.data.payload.transactionId`** in the postMessage listener.
2. **Get it from the bridge's redirect URL** — after the popup completes, its URL is
   `/complete/{transactionId}`. Read it from `window.opener` before close.

Poll responses match the same shape as the postMessage / webhook payload.

---

## 3. Result shape

```jsonc
{
  "transactionId": "897d2a74-8a0e-4e1d-aa5e-542416ae2d0a",
  "callerId":      "evokeconnect",
  "callerTxnId":   "your-uuid-here",
  "status":        "Acknowledged",   // Initiated, Sent, Returned, Acknowledged, Failed
  "createdAt":     "2026-05-18T13:34:22Z",
  "updatedAt":     "2026-05-18T13:34:32Z",
  "returnContext": "{\"uiRoute\":\"/cases/789\"}",   // echoed verbatim
  "mcgResponse": {
    "episodeId":   "EPS-20260518133431",
    "patientId":   "P-12345",
    "requestId":   "897d2a74-…",
    "patient": {
      "patientId":   "P-12345",
      "firstName":   "Jane",
      "lastName":    "Doe",
      "dateOfBirth": "01/15/1980",
      "gender":      "Female"
    },
    "episodeNotes": [
      { "author": "jdoe", "created": "2026-05-18T13:34:31Z", "text": "Notes..." }
    ],
    "guidelines": [
      {
        "guidelineId": "MOCK-G-001",
        "title": "Mock General Care Guideline",
        "outlines": [
          { "name": "Severity of illness documented", "status": "Met",       "notes": [] },
          { "name": "Intensity of service appropriate", "status": "NotMet",  "notes": [ { "text": "Insufficient." } ] }
        ]
      }
    ]
  },
  "mcgError":      null,    // populated on Failed
  "failureReason": null     // populated on Failed
}
```

Status flow:

```
Initiated ─▶ Sent ─▶ Returned ─▶ Acknowledged       (happy path)
                   ▶ Failed                         (MCG returned error envelope)
                   ▶ Failed (with reason)           (pipeline failure)
```

---

## 4. Errors

If MCG returns an error envelope (Dev Guide §7.1), `status` becomes `Failed`,
`mcgError` is populated with the parsed `<cwqierror>` content, and `failureReason`
contains a concatenated summary. The popup still closes; the listener / webhook /
poll endpoint all surface the same payload.

---

## 5. Versioning the contract

The launch URL parameter names and the result JSON shape are **the public API**.
Field-additive changes are non-breaking; renames / removals are breaking. Pin to
specific bridge releases in your deployment if you need strict guarantees.
