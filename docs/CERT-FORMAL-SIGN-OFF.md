# MCG CareWebQI Certification — Formal Sign-Off Form

Hand this form to the MCG cert reviewer (or use internally to attest readiness).
Each item references the *CareWebQI Certification Script Checklist v10.0*, Appendix A.

---

## Cover

| Field | Value |
|---|---|
| Organization | _________________________________ |
| Bridge version | _________________________________ |
| Bridge URL (production) | https://mcg-bridge.________________________ |
| MCG tenant URL | https://_________________.carewebqi.com |
| CareWebQI content version | _________________________________ |
| Cert date | __________ |
| Cert reviewer (MCG side) | _________________________________ |
| Engineering lead (our side) | _________________________________ |

---

## Section A — Tenant-side Application Settings (per cert prerequisites)

Confirm with your MCG Technical Support contact that the following are configured:

| Setting | Required | Confirmed (initial) |
|---|---|---|
| Documentable Definitions | True | ___ |
| Mandatory Variance | True | ___ |
| Use the Simplified Discharge Workflow | True | ___ |
| Enable Notes Deletion | On - Delete notes with strikethrough | ___ |
| Allow non-consecutive day documentation of recovery course | True | ___ |
| Interface: Hash Type | SHA256 or SHA512 | ___ |
| Interface: Response Type | RedirectOnly | ___ |
| Interface: Allow Patient Merge | True | ___ |
| Open discharged episode workflow | Auto-undischarge | ___ |

---

## Section B — Bridge configuration & deployment

| Item | Status | Initial |
|---|---|---|
| Bridge running on `net8.0`, all unit tests green | ☐ | ___ |
| YARP reverse proxy configured to upstream MCG tenant | ☐ | ___ |
| `Mcg:LoginKey` sourced from secret store (not source control) | ☐ | ___ |
| `Bridge:AllowedCallerOrigins` set to explicit caller origins (not `*`) | ☐ | ___ |
| `Bridge:PublicBaseUrl` matches the public TLS endpoint | ☐ | ___ |
| `db/schema.sql` applied to production SQL Server | ☐ | ___ |
| Iframe-blocking response headers stripped at proxy (X-Frame-Options, CSP, COOP) | ☐ | ___ |
| TLS termination + HTTPS-only redirect configured | ☐ | ___ |

---

## Section C — Cert script walkthrough

For each cert section, record the cert reviewer's pass/fail verdict.

### Section 1 — Create an episode (cert p68-69)

| Step | Description | Result | Reviewer initial |
|---|---|---|---|
| 1 | Launch CareWebQI with G44.001, Documentation type | ☐ Pass ☐ Fail | ___ |
| 2 | ICD-10 code passed, guideline search results displayed | ☐ Pass ☐ Fail | ___ |
| 3 | Partner episode ID matches CareWebQI episode ID | ☐ Pass ☐ Fail | ___ |
| 4 | Admission date in Actual Admit Date field | ☐ Pass ☐ Fail | ___ |
| 5 | Gender + address abbreviations pass through | ☐ Pass ☐ Fail | ___ |

### Section 2 — MCR guideline (cert p69-70)

| Step | Result | Reviewer initial |
|---|---|---|
| 1 — MCR guidelines first in search | ☐ Pass ☐ Fail | ___ |
| 2 — Add MCR-031 to episode | ☐ Pass ☐ Fail | ___ |
| 3 — Document covered + non-covered indications | ☐ Pass ☐ Fail | ___ |
| 4 — Partner-app validates response + episode summary | ☐ Pass ☐ Fail | ___ |
| 5 — Repeat for covered | ☐ Pass ☐ Fail | ___ |
| 6 — Repeat for covered + non-covered | ☐ Pass ☐ Fail | ___ |

### Section 3 — ISC guideline with M-282 + S-535 (cert p70-75)

| Step | Result | Reviewer initial |
|---|---|---|
| 1 — Add M-282; Benchmark/Print/Calculator open without error | ☐ Pass ☐ Fail | ___ |
| 2 — Document Clinical Indications, notes, definition, calculator | ☐ Pass ☐ Fail | ___ |
| 3 — Document Optimal Recovery Course Day 1 + variance | ☐ Pass ☐ Fail | ___ |
| 4 — Day 2 | ☐ Pass ☐ Fail | ___ |
| 5 — Episode notes | ☐ Pass ☐ Fail | ___ |
| 6 — Partner-app verifies episode info returned | ☐ Pass ☐ Fail | ___ |
| 7 — Add second guideline S-535 | ☐ Pass ☐ Fail | ___ |
| 8 — Document Optimal Recovery Course for S-535 | ☐ Pass ☐ Fail | ___ |
| 9 — Delete indication note (strikethrough) | ☐ Pass ☐ Fail | ___ |
| 10 — Delete clinical note | ☐ Pass ☐ Fail | ___ |
| 11 — Add non-consecutive day for second guideline | ☐ Pass ☐ Fail | ___ |
| 12 — Delete episode note | ☐ Pass ☐ Fail | ___ |
| 13 — Partner-app validates additional day | ☐ Pass ☐ Fail | ___ |
| 14 — Re-launch episode + add 3/19/2018 care date | ☐ Pass ☐ Fail | ___ |
| 15 — Partner-app validates deleted day NOT returned | ☐ Pass ☐ Fail | ___ |

### Section 4 — ISC with Mandatory Variance OFF (cert p75-79)

| Step | Result | Reviewer initial |
|---|---|---|
| All 11 steps | ☐ Pass ☐ Fail | ___ |

### Section 5 — Rapid Review Guideline (cert p79-80)

| Step | Result | Reviewer initial |
|---|---|---|
| 1 — Add M-190RRG | ☐ Pass ☐ Fail | ___ |
| 2 — Document Clinical Indications, Observation Care | ☐ Pass ☐ Fail | ___ |
| 3 — Discharge Readiness — multi-stage with variance + Discharge | ☐ Pass ☐ Fail | ___ |
| 4 — Add M-40RRG | ☐ Pass ☐ Fail | ___ |
| 5 — Discharge Readiness for M-40RRG | ☐ Pass ☐ Fail | ___ |
| 6 — Partner-app verifies Discharge Readiness criteria in XML | ☐ Pass ☐ Fail | ___ |

### Section 6 — General Recovery Care (cert p80-82)

| Step | Result | Reviewer initial |
|---|---|---|
| 1 — Launch new episode | ☐ Pass ☐ Fail | ___ |
| 2 — Add PG-ONC | ☐ Pass ☐ Fail | ___ |
| 3 — Document Clinical Indications | ☐ Pass ☐ Fail | ___ |
| 4 — Document General Recovery Course with variance | ☐ Pass ☐ Fail | ___ |
| 5–7 — PG-MTR variant | ☐ Pass ☐ Fail | ___ |
| 8 — Stage 2 documentation | ☐ Pass ☐ Fail | ___ |
| 9 — Partner-app verifies info | ☐ Pass ☐ Fail | ___ |

### Section 7 — Patient merge (cert p82-83)

| Step | Result | Reviewer initial |
|---|---|---|
| 1a–c — Launch + document + Exit with temp patient ID | ☐ Pass ☐ Fail | ___ |
| 1d — Update patient ID in partner app | ☐ Pass ☐ Fail | ___ |
| 1e–f — Re-launch + select No on merge prompt | ☐ Pass ☐ Fail | ___ |
| 1g — Verify "Episode can't be reassigned…" message | ☐ Pass ☐ Fail | ___ |
| 1h–i — Re-launch + select Yes to merge | ☐ Pass ☐ Fail | ___ |
| 1j — Verify corrected patient ID shown | ☐ Pass ☐ Fail | ___ |

---

## Section D — Sign-off

By signing below, MCG cert reviewer confirms the partner integration meets the
minimum requirements of CareWebQI Certification Checklist v10.0.

| Role | Name | Signature | Date |
|---|---|---|---|
| MCG Cert Reviewer | _____________________ | _____________________ | _____ |
| Partner Engineering Lead | _____________________ | _____________________ | _____ |
| Partner Business Owner | _____________________ | _____________________ | _____ |

**Notes / Conditions**:
```
_________________________________________________________________________________
_________________________________________________________________________________
_________________________________________________________________________________
```
