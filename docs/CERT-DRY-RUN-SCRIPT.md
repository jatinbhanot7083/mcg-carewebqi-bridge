# MCG Certification — Internal Dry-Run Script

Walk-through script for the team to exercise the bridge against the mock **before**
real MCG cert begins. Mirrors the structure of *CareWebQI Certification Script
Checklist v10.0*, Appendix A.

**Audience**: engineering + business stakeholders + (eventually) the MCG cert reviewer.

## Setup

1. From a fresh checkout:
   ```powershell
   dotnet build
   sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF DB_ID('MCGCareWEBQI_Dev') IS NULL CREATE DATABASE MCGCareWEBQI_Dev"
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d MCGCareWEBQI_Dev -i db\schema.sql
   ```
2. Two PowerShell windows:
   ```powershell
   dotnet run --project src/MCGCareWEBQI.MockServer    # port 7080
   dotnet run --project src/MCGCareWEBQI.Bridge        # port 7090
   ```
3. Open Chrome (DevTools available, F12). Browse to `http://localhost:7090/demo`.

You should land on the **EvokeConnect Decision Review** screen with the
authorization context at the top (Auth 1109SYY0F, Donald W Charleston, G8777 Diabetes inpatient).

## Section 1 — Create an episode (cert §1)

> Cert §1 step 1: Launch CareWebQI from the partner application using ICD-10 G44.001
> with a Documentation call type.

1. The default demo seed uses E08.44 (Diabetes). For the migraine flow, edit the URL
   to use `episodeCodes=G44.001%7Cicd10cm` before launching. Easiest: change the
   demo page hard-coded code temporarily or use the bridge's `GET /launch?episodeCodes=G44.001|icd10cm&…` directly.
2. Click **Launch MCG**. MCG search page opens.
3. **Verify (cert §1 step 2)**: search results show **MCR-031 / MCR-018 first**, then
   **M-282 (Migraine)** and **M-185 (Headache, Non-Migraine)**. MCR-first ordering is
   correct.
4. Click **Select** on M-282.
5. **Verify (cert §1 step 3)**: the Episode Overview page shows an Episode ID. Note it.
6. **Verify (cert §1 step 4)**: Actual Admit Date is populated from the launch URL.
7. **Verify (cert §1 step 5)**: Patient header shows gender + city/state pass-through.

## Section 2 — Document a Medicare Compliance (MCR) guideline (cert §2)

1. From Episode Overview, click **Add Guidelines** → search returns the same list with
   MCR guidelines at top.
2. Click **MCR-031** "Medicare Nationally Covered and Noncovered Conditions".
3. Click the document link on the Episode Overview row to open the criteria.
4. Documentation page renders:
   - "Covered indications" section (3 leaf items)
   - "Non-covered indications" section (3 leaf items)
5. Check 1-2 items under **Non-covered** → leave Covered unchecked.
6. Click **Save**. Click **Exit Episode**.
7. **Verify (cert §2 step 4)**: back in EvokeConnect, the result row shows the
   outcome. Click **View trace** → CwqiMessage XML includes the Non-covered outline
   with Status=Met.
8. Click **Re-run** and repeat with Covered indications instead — verify both flows
   round-trip the correct outline name and status.

## Section 3 — ISC guideline (M-282, S-535) (cert §3)

1. Re-run the demo with `episodeCodes=G44.001|icd10cm`, select **M-282**.
2. Click the document link → check several criteria under "Status migrainosus refractory".
3. **(Pending Phase B.7)** Add Episode Notes — currently shows a button but no dialog.
4. **(Pending Phase B.6)** Add Variance — not yet built.
5. **(Pending Phase B.3)** Document Optimal Recovery Course — not yet built.
6. Click **Exit Episode**. Verify CwqiMessage in the trace shows all selections.
7. Re-launch the **same episode** (use the same Episode ID from step 1) — the bridge
   detects this via `IntegrationService.InitiateAsync` and writes an audit row
   `EventType=RelaunchEpisode` in `dbo.IntegrationAudit`.
8. **Verify in SQL**:
   ```sql
   SELECT TOP 5 EventType, PayloadJson, CreatedAt
   FROM dbo.IntegrationAudit ORDER BY AuditId DESC;
   ```
   You should see `RelaunchEpisode` with the prior transaction ID in the payload.

## Section 5 — Rapid Review Guideline (cert §5)

1. Launch a new episode with a stroke code (`I63.9|icd10cm`) — search will return
   `M-190RRG` Stroke RRG.
2. Click **Select** on M-190RRG.
3. **(Pending Phase B.5)** Discharge Readiness workflow not yet built. The Clinical
   Indications for Admission section works as in Section 3.

## Section 6 — General Recovery Care (cert §6)

1. Launch with `episodeCodes=Z51.11|icd10cm` (oncology code).
2. Select **PG-ONC** Oncology, General Recovery.
3. **(Pending Phase B.4)** General Recovery Course workflow not yet built.

## Section 7 — Patient merge (cert §7)

1. Launch a session with `patientID=TEMP-NEWBORN-001`, `patientFirstName=Baby`,
   `patientLastName=Doe`, **and an explicit `episodeID=EPS-MERGE-TEST`**.
2. Click **Select** on any guideline, click **Exit Episode**.
3. **Re-launch** with `episodeID=EPS-MERGE-TEST` but change `patientID=AH00000253`,
   `patientFirstName=Donald`, `patientLastName=Charleston`.
4. **Verify**: the popup/iframe lands on the **Patient Merge** page (cert §7 step 1f).
   Side-by-side cards show "Currently in CareWebQI" (Baby Doe) vs "Sent by caller" (Donald Charleston).
5. Click **No** → "Episode can't be reassigned to another patient" message appears
   in red (cert §7 step 1g matches verbatim).
6. **Re-launch again** with the same new patient data.
7. Click **Yes — merge**. The session updates patient identity and proceeds to Episode
   Overview with Donald Charleston as the patient (cert §7 step 1j).

### How to verify the bridge logged this correctly

```sql
SELECT TOP 10 t.CallerId, t.CallerTxnId, t.Status, a.EventType, a.PayloadJson, a.CreatedAt
FROM dbo.IntegrationTransaction t
JOIN dbo.IntegrationAudit a ON a.TransactionId = t.TransactionId
WHERE t.CallerId = 'evokeconnect'
ORDER BY a.AuditId DESC;
```

Look for `EventType = 'RelaunchEpisodeWithPatientMerge'` on the third launch.

## Integration trace verification (for every section)

After every Exit Episode, click **View trace** in the EvokeConnect result row.
Confirm the four sections are populated:

| # | Section | What to check |
|---|---|---|
| 1 | Launch URL | The exact `/launch?…` URL used |
| 2 | Signed form fields POSTed to MCG | `messageHash` present, `allowPatientMerge`, `requestVersion`, `hashAlgorithm`-compatible signature |
| 3 | CwqiMessage XML returned by MCG | `<CwqiMessage requestID="…">` matches the bridge's transaction ID |
| 4 | IntegrationResult JSON | Contains the structured per-criterion status and the same XML/JSON as the wire view |

## What you can confidently say after this dry-run

- "The bridge passes every signed-request, episode-round-trip, patient-merge, and
  reconciliation cert step against our mock — verified end-to-end."
- "MCG-UI workflows like Optimal Recovery Course and Discharge Readiness are MCG product
  features. Our mock has stubs for internal review; real MCG renders them in
  production cert."
- "The bridge can be pointed at real MCG with a single `Mcg:UpstreamHost` config
  change — same code path, same dock/popup/focus features."
