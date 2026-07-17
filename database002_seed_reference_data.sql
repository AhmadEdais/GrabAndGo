SET NOCOUNT ON;
GO

MERGE dbo.SessionStatuses AS target
USING
(
    VALUES
        (1, N'Active'),
        (2, N'Ended'),
        (3, N'Abandoned')
) AS source (SessionStatusId, StatusName)
ON target.SessionStatusId = source.SessionStatusId
WHEN MATCHED THEN
    UPDATE SET StatusName = source.StatusName
WHEN NOT MATCHED THEN
    INSERT (SessionStatusId, StatusName)
    VALUES (source.SessionStatusId, source.StatusName);
GO

MERGE dbo.PaymentStatuses AS target
USING
(
    VALUES
        (1, N'Pending'),
        (2, N'Completed'),
        (3, N'Failed'),
        (4, N'Refunded')
) AS source (PaymentStatusId, StatusName)
ON target.PaymentStatusId = source.PaymentStatusId
WHEN MATCHED THEN
    UPDATE SET StatusName = source.StatusName
WHEN NOT MATCHED THEN
    INSERT (PaymentStatusId, StatusName)
    VALUES (source.PaymentStatusId, source.StatusName);
GO

MERGE dbo.ProcessingStatuses AS target
USING
(
    VALUES
        (1, N'Pending'),
        (2, N'Applied'),
        (3, N'Rejected')
) AS source (ProcessingStatusId, StatusName)
ON target.ProcessingStatusId = source.ProcessingStatusId
WHEN MATCHED THEN
    UPDATE SET StatusName = source.StatusName
WHEN NOT MATCHED THEN
    INSERT (ProcessingStatusId, StatusName)
    VALUES (source.ProcessingStatusId, source.StatusName);
GO

MERGE dbo.LedgerEntryTypes AS target
USING
(
    VALUES
        (1, N'TopUp'),
        (2, N'Debit'),
        (3, N'Refund')
) AS source (LedgerEntryTypeId, TypeName)
ON target.LedgerEntryTypeId = source.LedgerEntryTypeId
WHEN MATCHED THEN
    UPDATE SET TypeName = source.TypeName
WHEN NOT MATCHED THEN
    INSERT (LedgerEntryTypeId, TypeName)
    VALUES (source.LedgerEntryTypeId, source.TypeName);
GO