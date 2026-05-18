-- Optional seed data for local development / smoke-test demos.
-- Re-runnable: deletes any prior demo rows first.

DELETE FROM dbo.IntegrationAudit       WHERE TransactionId IN
    (SELECT TransactionId FROM dbo.IntegrationTransaction WHERE CallerId = 'demo-seed');
DELETE FROM dbo.IntegrationTransaction WHERE CallerId = 'demo-seed';

DECLARE @txn UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.IntegrationTransaction
    (TransactionId, CallerId, CallerTxnId, Status, RequestParamsJson, CreatedAt, UpdatedAt)
VALUES
    (@txn, 'demo-seed', 'SEED-0001', 'Acknowledged',
     N'{"patientId":"P-1001","patientFirstName":"Jane","patientLastName":"Doe","episodeType":"Inpatient"}',
     SYSUTCDATETIME(), SYSUTCDATETIME());

INSERT INTO dbo.IntegrationAudit (TransactionId, EventType, PayloadJson, CreatedAt)
VALUES (@txn, 'LaunchReceived',         N'{}', SYSUTCDATETIME()),
       (@txn, 'PostedToMcg',            N'{}', SYSUTCDATETIME()),
       (@txn, 'McgResponseReceived',    N'{}', SYSUTCDATETIME()),
       (@txn, 'ReconcileAcknowledged',  N'{}', SYSUTCDATETIME());
