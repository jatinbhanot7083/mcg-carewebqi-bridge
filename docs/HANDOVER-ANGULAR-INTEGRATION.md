# Handover — Integrating the MCG Bridge from Angular (EvokeConnect)

For the Angular + .NET dev team. This document explains exactly what to add to the
EvokeConnect Angular codebase to invoke the bridge.

> **Short answer**: a single TypeScript service (~80 lines) + one optional
> component for inline dock-in-panel rendering. The bridge stays in .NET; the
> Angular app calls it via a documented HTTP launch URL and listens for results
> via `window.message` or `BroadcastChannel`.

---

## 1. Architecture you're integrating with

```
┌────────────────────────┐
│ EvokeConnect (Angular) │     1. window.open(launchUrl, ...)
│  Auth Decision screen  │ ────────────────────────────────────┐
└────────────────────────┘                                      ▼
            ▲                                       ┌─────────────────────┐
            │     4. postMessage / BroadcastChannel │  MCG Bridge (.NET)  │
            └───────────────────────────────────────│  + YARP proxy       │ ── /__mcg/* ──▶ MCG
                                                    │  + SQL persistence   │
                                                    └─────────────────────┘
```

You write code only in the dotted box on the left. Everything else is the bridge,
already built.

---

## 2. The integration contract (HTTP + query string + JSON)

### Launch URL (Angular → Bridge)

```
GET https://mcg-bridge.your-org.com/launch
    ?callerId=evokeconnect
    &callerTxnId=<your-correlation-uuid>
    &patientId=AH00000253
    &patientFirstName=Donald
    &patientLastName=Charleston
    &patientDateOfBirth=01/15/1980
    &patientGender=Male
    &episodeType=Inpatient
    &episodeAdmitDate=05/19/2026
    &episodeCodes=E08.44|icd10cm
    [&callbackUrl=https://...]      # optional server callback
    [&returnContext=<any-string>]   # optional, echoed back
    [&dock=true]                    # set ONLY for inline-iframe mode
```

Full field list: [`LaunchRequest.cs`](../src/MCGCareWEBQI.Shared/Models/Launch/LaunchRequest.cs).

### Result shape (Bridge → Angular)

Delivered via `window.postMessage` OR `BroadcastChannel('mcg-dock')` OR webhook
OR REST poll. Payload shape:

```typescript
interface McgResult {
  transactionId: string;          // Bridge-assigned UUID
  callerId: string;
  callerTxnId: string;            // YOUR uuid, echoed back verbatim
  status: 'Initiated' | 'Sent' | 'Returned' | 'Acknowledged' | 'Failed';
  createdAt: string;              // ISO 8601
  updatedAt: string;
  returnContext: string | null;   // echoed verbatim if you sent it on launch
  mcgResponse: {
    episodeId: string;
    patientId: string;
    requestId: string;
    patient: { patientId: string; firstName: string; lastName: string; dateOfBirth: string; gender: string; };
    episodeNotes: Array<{ author: string; created: string; text: string }>;
    guidelines: Array<{
      guidelineId: string;
      title: string;
      outlines: Array<{ name: string; status: 'Met' | 'NotMet' | 'Unset'; notes: any[] }>;
    }>;
  } | null;
  mcgError: { type: string; messages: Array<{ code: string; text: string }> } | null;
  failureReason: string | null;
  // Developer-visibility extras (for debugging — not for production UX):
  launchUrl: string;
  outboundFieldsJson: string;
  mcgResponseXml: string;
}
```

---

## 3. Angular service (drop this in)

`src/app/integrations/mcg-bridge/mcg-bridge.service.ts`

```typescript
import { Injectable, NgZone } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface McgLaunchParams {
  callerId: string;            // 'evokeconnect'
  callerTxnId: string;         // your correlation UUID
  patientId?: string;
  patientFirstName?: string;
  patientLastName?: string;
  patientMI?: string;
  patientDateOfBirth?: string; // 'mm/dd/yyyy'
  patientGender?: string;
  patientBenefitPlanName?: string;
  episodeId?: string;          // omit on new episode; MCG assigns
  episodeType?: string;        // 'Inpatient' etc.
  episodeAdmitDate?: string;   // 'mm/dd/yyyy'
  episodeCodes?: string;       // 'E08.44|icd10cm$I10|icd10cm' (Dev Guide §4.3)
  facilityId?: string;
  facilityName?: string;
  attendingProviderId?: string;
  pcpId?: string;
  documentingUser?: string;    // defaults to 'Api-User' on bridge if omitted
  returnContext?: string;      // any string — echoed back to you
  callbackUrl?: string;        // optional server-to-server webhook
  requestType?: 'documentation' | 'episodesummary' | 'guideline' | 'discharge';
}

export interface McgResult {
  transactionId: string;
  callerId: string;
  callerTxnId: string;
  status: 'Initiated' | 'Sent' | 'Returned' | 'Acknowledged' | 'Failed';
  createdAt: string;
  updatedAt: string;
  returnContext: string | null;
  mcgResponse: any | null;
  mcgError: any | null;
  failureReason: string | null;
  launchUrl?: string;
  outboundFieldsJson?: string;
  mcgResponseXml?: string;
}

@Injectable({ providedIn: 'root' })
export class McgBridgeService {
  private readonly bridgeBaseUrl = environment.mcgBridgeUrl; // e.g. https://mcg-bridge.your-org.com
  private popup: Window | null = null;
  private channel = ('BroadcastChannel' in window) ? new BroadcastChannel('mcg-dock') : null;

  constructor(private zone: NgZone) {}

  /** Build the full launch URL with optional dock=true. */
  buildLaunchUrl(params: McgLaunchParams, dock = false): string {
    const u = new URL(`${this.bridgeBaseUrl}/launch`);
    Object.entries(params).forEach(([k, v]) => {
      if (v !== undefined && v !== null && v !== '') u.searchParams.set(k, String(v));
    });
    if (dock) u.searchParams.set('dock', 'true');
    return u.toString();
  }

  /** Launch in a popup window. Returns an Observable that emits exactly once with the result. */
  launchPopup(params: McgLaunchParams): Observable<McgResult> {
    return new Observable<McgResult>(subscriber => {
      const url = this.buildLaunchUrl(params, /*dock*/ true); // dock=true so popup is proxied + same-origin

      const onMessage = (e: MessageEvent) => {
        if (!e.data || e.data.source !== 'mcg-bridge') return;
        const payload = e.data.payload as McgResult;
        if (payload.callerTxnId !== params.callerTxnId) return; // ignore other launches
        this.zone.run(() => { subscriber.next(payload); subscriber.complete(); });
      };

      window.addEventListener('message', onMessage);
      this.channel?.addEventListener('message', onMessage as any);

      this.popup = window.open(url, 'mcg-popup', 'width=1280,height=860,menubar=no,toolbar=no');
      if (!this.popup) {
        subscriber.error(new Error('Popup blocked — allow popups for the bridge origin.'));
        return;
      }

      // Cleanup
      return () => {
        window.removeEventListener('message', onMessage);
        this.channel?.removeEventListener('message', onMessage as any);
        if (this.popup && !this.popup.closed) try { this.popup.close(); } catch { /* ignore */ }
      };
    });
  }

  /** Poll for status. Useful if you launched a session you've lost track of. */
  async pollResult(transactionId: string): Promise<McgResult | null> {
    const r = await fetch(`${this.bridgeBaseUrl}/api/transactions/${transactionId}`);
    if (r.status === 404) return null;
    if (!r.ok) throw new Error(`Bridge poll failed: HTTP ${r.status}`);
    return await r.json() as McgResult;
  }

  newCorrelationId(): string {
    // Use whatever you already use for UUIDs; this is a fallback.
    return (crypto as any).randomUUID
      ? (crypto as any).randomUUID()
      : `${Date.now()}-${Math.random().toString(36).slice(2)}`;
  }
}
```

`src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  mcgBridgeUrl: 'http://localhost:7090'   // dev
};
```

`src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  mcgBridgeUrl: 'https://mcg-bridge.your-org.com'   // prod
};
```

---

## 4. Use it from a component (popup mode)

```typescript
import { Component } from '@angular/core';
import { McgBridgeService, McgLaunchParams } from './integrations/mcg-bridge/mcg-bridge.service';

@Component({
  selector: 'app-auth-decision',
  template: `
    <button (click)="launchMcg()">+ Add MCG</button>
    <div *ngIf="result">
      <span>Outcome: {{ result.status }}</span>
      <span *ngIf="result.mcgResponse">Episode: {{ result.mcgResponse.episodeId }}</span>
    </div>
  `
})
export class AuthDecisionComponent {
  result: McgResult | null = null;
  constructor(private mcg: McgBridgeService) {}

  launchMcg(): void {
    const params: McgLaunchParams = {
      callerId: 'evokeconnect',
      callerTxnId: this.mcg.newCorrelationId(),
      patientId: this.member.id,
      patientFirstName: this.member.firstName,
      patientLastName: this.member.lastName,
      patientDateOfBirth: this.member.dob,           // mm/dd/yyyy
      patientGender: this.member.gender,
      episodeType: 'Inpatient',
      episodeAdmitDate: this.auth.fromDate,          // mm/dd/yyyy
      episodeCodes: this.auth.diagnosisCode + '|icd10cm',
      documentingUser: this.currentUser.username,
      returnContext: JSON.stringify({ authId: this.auth.id })
    };

    this.mcg.launchPopup(params).subscribe({
      next: result => {
        this.result = result;
        // Attach result.status to your decision record:
        this.decisionService.attachMcg(this.auth.id, result);
      },
      error: err => console.error('MCG launch failed', err)
    });
  }
}
```

---

## 5. Use it from a component (dock-in-panel / iframe mode — recommended)

`mcg-dock.component.ts`

```typescript
import { Component, Input, Output, EventEmitter, OnDestroy, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { McgBridgeService, McgLaunchParams, McgResult } from '../integrations/mcg-bridge/mcg-bridge.service';

@Component({
  selector: 'app-mcg-dock',
  template: `
    <div class="mcg-panel">
      <div class="mcg-panel-hd">
        <span>MCG CareWebQI — live session</span>
        <span style="flex:1"></span>
        <button (click)="toggleFocus()">{{ focus ? 'Exit focus' : 'Focus' }}</button>
        <button (click)="popOut()">Pop out</button>
        <button (click)="cancel()">Cancel</button>
      </div>
      <iframe *ngIf="safeUrl" [src]="safeUrl"
              [style.height]="focus ? 'calc(100vh - 120px)' : '780px'"
              style="width:100%; border:none; background:#fff;"></iframe>
    </div>
  `,
  styles: [`
    .mcg-panel { display:flex; flex-direction:column; border:1px solid #e2e8f0; border-radius:8px; overflow:hidden; }
    .mcg-panel-hd { background:#0f172a; color:#e2e8f0; padding:8px 14px; display:flex; gap:10px; }
  `]
})
export class McgDockComponent implements OnInit, OnDestroy {
  @Input() launchParams!: McgLaunchParams;
  @Output() complete = new EventEmitter<McgResult>();
  @Output() cancelled = new EventEmitter<void>();

  safeUrl: SafeResourceUrl | null = null;
  focus = false;
  private channel?: BroadcastChannel;
  private boundHandler = this.onMessage.bind(this);

  constructor(private mcg: McgBridgeService, private sanitizer: DomSanitizer) {}

  ngOnInit() {
    const url = this.mcg.buildLaunchUrl(this.launchParams, /*dock*/ true);
    this.safeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(url);

    window.addEventListener('message', this.boundHandler);
    if ('BroadcastChannel' in window) {
      this.channel = new BroadcastChannel('mcg-dock');
      this.channel.addEventListener('message', this.boundHandler as any);
    }
  }

  ngOnDestroy() {
    window.removeEventListener('message', this.boundHandler);
    this.channel?.close();
  }

  toggleFocus() { this.focus = !this.focus; }

  popOut() {
    // Open same URL in a popup; iframe stays in case user comes back.
    const url = this.mcg.buildLaunchUrl(this.launchParams, /*dock*/ true);
    window.open(url, 'mcg-popup', 'width=1280,height=860');
  }

  cancel() { this.cancelled.emit(); }

  private onMessage(e: MessageEvent) {
    if (!e.data || e.data.source !== 'mcg-bridge') return;
    const payload = e.data.payload as McgResult;
    if (payload.callerTxnId !== this.launchParams.callerTxnId) return;
    this.complete.emit(payload);
  }
}
```

Use it:

```html
<app-mcg-dock *ngIf="mcgRunning"
              [launchParams]="mcgParams"
              (complete)="onMcgComplete($event)"
              (cancelled)="mcgRunning = false">
</app-mcg-dock>
```

---

## 6. Server-to-server webhook (alternative — Angular not involved)

If the calling app prefers backend-side notification:

1. On the launch URL include `&callbackUrl=https://your-api/mcg/callback`.
2. Bridge POSTs the same `McgResult` JSON to that URL when MCG returns.
3. Your `.NET` backend writes it to your DB.
4. Your Angular UI polls or uses SignalR to learn the result.

The bridge retries the POST up to `Bridge:CallbackRetryCount` times on non-2xx
response (default 3, exponential backoff).

---

## 7. REST polling (alternative — fallback when no popup / no opener)

If you've lost track of an in-flight session (e.g., browser tab reload mid-flow):

```typescript
const result = await mcg.pollResult(transactionId);
if (result && result.status === 'Acknowledged') { ... }
```

---

## 8. Deployment topology

```
                     internet
                        │
                        ▼
              ┌─────────────────────┐
              │  Your reverse proxy │  (nginx / Azure Front Door / etc.)
              └─────────────────────┘
                  │             │
                  ▼             ▼
        evokeconnect.your   mcg-bridge.your
        -org.com            -org.com
        (Angular SPA +      (this .NET 10 ASP.NET Core app)
         your .NET API)
                                │
                                ▼
                       https://tenant.carewebqi.com
                       (real MCG, when keys arrive)
```

- **EvokeConnect** stays in its current cluster.
- **MCG Bridge** is a NEW ASP.NET Core service on its own host/container. Standalone.
- They communicate via the documented HTTP contract — no shared DLLs, no shared DB.
- `Bridge:AllowedCallerOrigins` is set to your Angular SPA origin so `postMessage` is restricted.

---

## 9. CORS / origin notes

The bridge does NOT need CORS for the popup or iframe flow — those navigations are
top-level browser navigations, not XHR. The only fetch from your Angular code is
`mcg.pollResult()` which hits `/api/transactions/{id}`. If your Angular app and the
bridge live on different origins (typical), enable CORS on the bridge:

```csharp
// Bridge Program.cs — add to existing service configuration
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration["Bridge:AllowedCallerOrigins"]!.Split(','))
     .AllowAnyHeader().AllowAnyMethod()));

// ...later:
app.UseCors();
```

(Not currently added by default — turn on when your Angular app pings the poll
endpoint from a different origin.)

---

## 10. Authentication

The bridge handles **MCG-side auth** (interface key + signed POST) internally.
You don't need to do anything for that.

**Your-side auth** (who is allowed to call the bridge): the bridge does not check
the caller's identity today. Recommended setup:

- Put both EvokeConnect and the bridge behind your existing identity gateway (Azure AD,
  Okta, your SSO).
- The bridge accepts JWT bearer tokens; configure standard ASP.NET Core JWT auth
  middleware on `/launch`, `/receive`, `/api/transactions/*`.
- Angular sends the user's token on the launch URL via a header (or signed cookie)
  — but note that browser popups don't send custom headers. The most practical
  pattern is **same-origin cookies**: deploy the bridge under a subdomain of your
  app domain so the user's auth cookie flows automatically.

If you want me to add JWT or cookie-based auth to the bridge as a follow-up,
ask — it's a 1-day add.

---

## 11. What you build vs what you reuse

| Component | Status | Where it lives |
|---|---|---|
| MCG signed POST + hash | ✅ reuse | `MCGCareWEBQI.Shared/Hashing/CwqiHash.cs` |
| `CwqiMessage` / `CwqiError` parsing | ✅ reuse | `MCGCareWEBQI.Shared/Models/Mcg/*.cs` |
| Reverse proxy to MCG | ✅ reuse | `MCGCareWEBQI.Bridge/Program.cs` (YARP) |
| `IntegrationTransaction` persistence | ✅ reuse | `MCGCareWEBQI.Data/*` |
| Patient merge / episode re-launch | ✅ reuse | bridge handles it server-side |
| Dock / popup / focus UI mechanics | ✅ reuse | bridge serves the popup-frame + iframe pages |
| `McgBridgeService` (Angular) | 📝 you write | ~80 lines, sample above |
| `McgDockComponent` (Angular) | 📝 you write | ~50 lines, sample above |
| Wire-up in your Auth Decision screen | 📝 you write | 1 button + 1 callback handler |
| Mock MCG (`MCGCareWEBQI.MockServer`) | 🗑️ dev-only | Don't deploy to production |
| Demo page (`/demo` Blazor in bridge) | 🗑️ dev-only | Don't deploy to production |

The mock and demo are **reference implementations** — they exist so engineers can
see exactly what to build. Production deploys only the bridge (no Blazor demo
page, no mock server).

To strip them for production:

```powershell
# In MCGCareWEBQI.Bridge.csproj — comment out the Components folder and remove MudBlazor reference
# Stop hosting /demo — leave only the API endpoints (/launch, /receive, /api/*, /popup-frame, /__mcg/*)
```

Or leave them in and just don't link to them — they're harmless if no one navigates to /demo.

---

## 12. Production readiness checklist

Before going live:

- [ ] Bridge deployed to its own host with HTTPS
- [ ] `Mcg:LoginKey` from secret store, not source control
- [ ] `Bridge:AllowedCallerOrigins` set to explicit Angular origin(s), not `*`
- [ ] `Mcg:UpstreamHost` set to real MCG tenant URL
- [ ] `db/schema.sql` applied to production SQL Server
- [ ] CORS enabled on bridge if Angular calls `/api/transactions/*` cross-origin
- [ ] Auth strategy in place (cookie or JWT — see §10)
- [ ] [`docs/PRODUCTION-DEPLOYMENT.md`](PRODUCTION-DEPLOYMENT.md) checklist complete
- [ ] [`docs/CERT-PREREQ.md`](CERT-PREREQ.md) MCG-tenant settings confirmed by MCG Tech Support
- [ ] [`docs/CERT-DRY-RUN-SCRIPT.md`](CERT-DRY-RUN-SCRIPT.md) walkthrough completed in staging

---

## 13. Questions you'll get from your team

| Q | A |
|---|---|
| Should we rewrite this in Angular? | **No.** Bridge is .NET, Angular calls it. ~130 lines of TypeScript in the Angular side. |
| Can we skip the bridge and call MCG directly from Angular? | **No.** MCG requires server-side hash signing (you can't hide the interface key in a browser), SOAP ACK, episode persistence — all server-side concerns. |
| Why not just iframe MCG directly into Angular? | Cross-origin cookies → MCG's auth breaks. Bridge proxies under your own domain so cookies are first-party. |
| Can we use the bridge for InterQual / other guideline vendors too? | Architecturally yes — the bridge is vendor-neutral on its caller side. Adding a second upstream would mean a second proxy route + a second `Mcg2Options` config block. Reasonable follow-up. |
| What's the SLA / uptime impact? | Bridge becomes a critical path between EvokeConnect and MCG. Treat its uptime like MCG's — same SLA target, same monitoring, horizontal scale if needed (YARP is stateless). |
| Is there a JavaScript SDK we could publish? | The Angular service in §3 is the SDK. ~80 lines. Don't over-engineer. |

---

## 14. Support / questions

Anything not covered here: check the other docs first:
- [`docs/INTEGRATION.md`](INTEGRATION.md) — caller-agnostic contract
- [`docs/CONFIG.md`](CONFIG.md) — every config key
- [`docs/PRODUCTION-DEPLOYMENT.md`](PRODUCTION-DEPLOYMENT.md) — prod swap
- [`docs/CERT-CHECKLIST.md`](CERT-CHECKLIST.md) — cert readiness

If something's wrong with the bridge itself (not the Angular integration), the
bridge log in `src/MCGCareWEBQI.Bridge/logs/bridge-{date}.log` captures every
request and every state transition.
