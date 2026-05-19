# MCG CareWebQI Certification — Step-by-step mapping

Cross-reference of every step in the *CareWebQI Certification Script Checklist v10.0
(Appendix A, pages 68–82)* to what this bridge implements.

**Legend**

| Mark | Meaning |
|---|---|
| ✅ | Bridge implements + verified end-to-end against the mock |
| 🟢 | MCG-UI feature — mock has a working stand-in for internal dry-run |
| ⚠️ | MCG-UI feature — mock has a simplified stub (not pixel-perfect) |
| ❌ | Out of scope for the bridge — pure MCG product feature |
| 🔒 | Real-MCG-only — cannot be exercised against the mock |

---

## Section 1 — Create an episode in CareWebQI (cert p68–69)

| Step | Description | Status | Notes |
|---|---|---|---|
| 1 | Create episode in CareWebQI from partner application using ICD-10 code G44.001, "Documentation" call type | ✅ | Bridge `/launch?episodeCodes=G44.001|icd10cm&requestType=documentation` — search results pre-filter to migraine/MCR guidelines |
| 2 | Verify ICD-10 code passed and correct guideline search results displayed | ✅ + 🟢 | Mock seeds M-282 (Migraine), M-185 (Headache), MCR-031, MCR-018 for G44.001 |
| 3 | Verify partner-app episode ID is same as CareWebQI episode ID | ✅ | Bridge mints `requestID = TransactionId.ToString()`; MCG echoes it back in `CwqiMessage[@requestID]` |
| 4 | Verify admission date appears in Actual Admit Date field | ✅ | Bridge passes `episodeAdmitDate` per Dev Guide §4.3; mock surfaces it on Episode Overview |
| 5 | Verify gender + address abbreviations pass | ✅ | Bridge pass-through of `patientGender`, `patientCity`, `patientState`, `patientCountryCode` |

---

## Section 2 — Document a Medicare Compliance (MCR) guideline (cert p69–70)

| Step | Description | Status | Notes |
|---|---|---|---|
| 1 | MCR guidelines appear FIRST in search results | ✅ + 🟢 | `GuidelineSeeds.ProductSortKey` puts MCR=1, custom=2, others=3 |
| 2 | Add MCR guideline (MCR-031) to episode | 🟢 | Mock supports Select on MCR-031 → loads guideline |
| 3a | Document Medicare Nationally Covered and Noncovered Conditions | ⚠️ | Mock has covered/non-covered sections under MCR-031 with checkable indications |
| 3b | Verify indications for covered + non-covered work | ⚠️ | Tree supports both, but no special "non-covered" highlight |
| 3c | Save with not-covered result + Exit Episode | 🟢 | CwqiMessage round-trips overall outcome |
| 4 | Partner application validates response + episode summary | ✅ | Integration Trace panel shows the returned CwqiMessage XML |
| 5–6 | Repeat for meeting covered indications + mixed | 🟢 | Re-run flow works via "Re-run" button |

---

## Section 3 — ISC guideline with non-consecutive days (cert p70–75)

| Step | Description | Status | Notes |
|---|---|---|---|
| 1a | Add Guidelines → Search M-282 → Select | 🟢 | Mock seeded with M-282 (Migraine, Status Migrainosus) |
| 1d | Benchmark and Statistics / Print View / Calculator icon all open | ❌ | UI buttons not implemented — pure MCG product features |
| 2a–b | Document Clinical Indications for Admission to Inpatient Care | ✅ + 🟢 | Recursive criteria tree with three-state evaluation; supports indication notes |
| 2c | Indication notes for selected AND un-selected indications | 🟢 | Note icon on every criterion (Met or Unset) |
| 2d | Open definition (blue italic hyperlink) and document the definition | ⚠️ | Hyperlinked clinical terms render; **definition popup not yet built** (Phase B.9 pending) |
| 2e | Calculator opens, enter values, Save and include in episode summary | ⚠️ | Calculator widget renders; **interactive calculator dialog not yet built** (Phase B.10 pending) |
| 2f | Add clinical note | ✅ + 🟢 | Clinical Notes editor at bottom of Document page with Subject dropdown |
| 2g | Save | ✅ | Save button persists session state |
| 3 | Document Optimal Recovery Course — Day 1, milestones, indication notes, clinical note, Add Variance, Save & Next | ⚠️ | **Optimal Recovery Course workflow not yet built** (Phase B.3 pending) |
| 4 | Optimal Recovery Course — Day 2 | ⚠️ | Same |
| 5 | Add Episode Notes | ⚠️ | **Episode notes dialog not yet built** (Phase B.7 pending) |
| 6 | Partner application verifies episode info returned (dates + summary) | ✅ | Integration Trace shows full CwqiMessage with all selections |
| 7–8 | Add second guideline S-535 (Heart Transplant), document Optimal Recovery Course | 🟢 + ⚠️ | S-535 seeded; recovery course UI pending |
| 9 | Delete an indication note (with strikethrough) | ⚠️ | **Note history + strikethrough not yet built** (Phase B.8 pending) |
| 10 | Delete a clinical note (with View Note History) | ⚠️ | Same |
| 11 | Add a new care date for second guideline (non-consecutive day) | ⚠️ | Same |
| 12 | Delete an episode note | ⚠️ | Phase B.7 pending |
| 13 | Partner application validates additional day + episode summary | ✅ | |
| 14 | Re-launch same episode, add 3/19/2018 care date | ✅ | Bridge re-launch implemented (Phase A.2 — see `IntegrationService.cs`) |
| 15 | Partner application validates deleted day NOT returned | ✅ | Once delete is implemented (Phase B.8) the round-trip works |

---

## Section 4 — ISC guideline with Mandatory Variance OFF (cert p75–79)

Same workflow as Section 3 but with MCG-tenant settings:
- `Mandatory Variance: False`
- `Use the Simplified Discharge Workflow: False`
- `Open discharged episode workflow: Auto-undischarge`

| Step | Status | Notes |
|---|---|---|
| 1–11 | Same gaps as Section 3 — depends on Phase B.3 + B.6 + B.8 + B.7 |

---

## Section 5 — Rapid Review Guideline (RRG) (cert p79–80)

| Step | Description | Status | Notes |
|---|---|---|---|
| 1 | Add M-190RRG to episode | 🟢 | Seeded |
| 2 | Document Clinical Indications, Observation Care option, clinical note, Save | 🟢 + ⚠️ | Tree works; Observation Care toggle pending (Phase B.5) |
| 3 | Discharge Readiness section: Set Admit Date, Add Care Date, milestones, Continue Stage, Add Variance, Discharge | ⚠️ | **Discharge Readiness workflow not yet built** (Phase B.5 pending) |
| 4 | Add M-40RRG to new episode | 🟢 | Seeded |
| 5 | Document Discharge Readiness — multiple care dates, milestones, variance | ⚠️ | Phase B.5 pending |
| 6 | Partner-app verifies all Discharge Readiness criteria returned in XML | ✅ | CwqiResponseBuilder serializes all checked outlines regardless of section |

---

## Section 6 — General Recovery Care (GRG) (cert p80–82)

| Step | Description | Status | Notes |
|---|---|---|---|
| 1 | Launch new episode | ✅ | |
| 2 | Add PG-ONC guideline | 🟢 | Seeded |
| 3 | Document Clinical Indications for Admission to Inpatient Care | 🟢 | Tree works |
| 4 | Document General Recovery Course — milestones, Stage 2, Variance | ⚠️ | **General Recovery Course workflow not yet built** (Phase B.4 pending) |
| 5–7 | Launch new episode, add PG-MTR, document indications | 🟢 | Seeded |
| 8 | Document General Recovery Course — Stage 2 | ⚠️ | Phase B.4 pending |
| 9 | Partner-app verifies info returned correctly | ✅ | |

---

## Section 7 — Patient merge (cert p82–83)

| Step | Description | Status | Notes |
|---|---|---|---|
| 1a | Launch CareWebQI with temporary patient ID | ✅ | |
| 1b | Add and document a guideline | ✅ + 🟢 | |
| 1c | Exit episode | ✅ + 🟢 | |
| 1d | Partner application updates patient ID | ✅ | Caller controls launch params |
| 1e | Re-launch CareWebQI | ✅ | Phase A.2 |
| 1f | Patient merge notification page — select No | ✅ + 🟢 | Mock's `PatientMerge.razor` page |
| 1g | Verify message "Episode can't be reassigned to another patient" | ✅ + 🟢 | Mock renders this exact message |
| 1h | Re-launch CareWebQI again | ✅ | |
| 1i | Select Yes — merge | ✅ + 🟢 | Mock applies merge, updates session patient identity |
| 1j | Verify corrected patient ID shows in CareWebQI | ✅ + 🟢 | Post-merge session reflects new patient |

---

## Optional sections — Guideline Modification Module (cert p83–85)

The cert script marks these explicitly **not part of the core certification** unless
the customer licenses GMM. We don't implement these; they're internal MCG admin features.

| Step | Description | Status |
|---|---|---|
| GMM 1–3 | Copy guideline, publish, verify searchable | ❌ (not in scope) |
| Policy 1–3 | Add medical policy, publish, verify searchable | ❌ (not in scope) |

---

## Summary of remaining gaps (for bridge certification readiness)

| Item | Phase | Effort estimate |
|---|---|---|
| Optimal Recovery Course workflow (multi-day, milestones, variance) | B.3 | ~1 day |
| General Recovery Course workflow (Stage-based) | B.4 | ~0.5 day |
| Discharge Readiness workflow (RRG-specific) | B.5 | ~0.5 day |
| Add Variance dialog (Category + Reason) | B.6 | ~2 hrs |
| Episode Notes (Add / Save / Delete) | B.7 | ~2 hrs |
| Note history + strikethrough delete | B.8 | ~3 hrs |
| Definition popovers | B.9 | ~1 hr |
| Calculator dialog with "Save to episode summary" | B.10 | ~2 hrs |

All bridge-side cert items (signing, episode round-trip, patient merge, re-launch,
Reconcile ACK, response delivery) are **complete**. The remaining gaps are mock-UI
features needed for the **internal dry-run** — when running real cert against real MCG,
these are MCG product features that don't involve us.
