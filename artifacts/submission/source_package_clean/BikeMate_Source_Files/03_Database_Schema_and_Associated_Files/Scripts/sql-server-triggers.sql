/* BikeMate SQL Server trigger pack. Run after EF migrations create the tables. */

CREATE OR ALTER TRIGGER trg_reviews_update_mechanic_rating
ON dbo.reviews
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE m
    SET
        AverageRating = ISNULL(r.avg_rating, 0),
        UpdatedAt = SYSUTCDATETIME()
    FROM dbo.mechanics m
    LEFT JOIN (
        SELECT MechanicId, CAST(AVG(CAST(Rating AS DECIMAL(3,2))) AS DECIMAL(3,2)) AS avg_rating
        FROM dbo.reviews
        GROUP BY MechanicId
    ) r ON m.MechanicId = r.MechanicId
    WHERE m.MechanicId IN (
        SELECT MechanicId FROM inserted
        UNION
        SELECT MechanicId FROM deleted
    );
END;
GO

CREATE OR ALTER TRIGGER trg_requests_update_completed_jobs
ON dbo.service_requests
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE m
    SET
        TotalCompletedJobs = ISNULL(x.completed_jobs, 0),
        UpdatedAt = SYSUTCDATETIME()
    FROM dbo.mechanics m
    LEFT JOIN (
        SELECT sr.MechanicId, COUNT(*) AS completed_jobs
        FROM dbo.service_requests sr
        INNER JOIN dbo.request_statuses rs ON sr.CurrentStatusId = rs.StatusId
        WHERE rs.StatusName = 'completed'
          AND sr.MechanicId IS NOT NULL
        GROUP BY sr.MechanicId
    ) x ON m.MechanicId = x.MechanicId
    WHERE m.MechanicId IN (
        SELECT MechanicId FROM inserted WHERE MechanicId IS NOT NULL
        UNION
        SELECT MechanicId FROM deleted WHERE MechanicId IS NOT NULL
    );
END;
GO

CREATE OR ALTER TRIGGER trg_service_request_status_history
ON dbo.service_requests
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.request_status_history (
        RequestId,
        OldStatusId,
        NewStatusId,
        ChangedByUserId,
        Notes,
        CreatedAt
    )
    SELECT
        i.RequestId,
        d.CurrentStatusId,
        i.CurrentStatusId,
        NULL,
        'Status automatically recorded by trigger.',
        SYSUTCDATETIME()
    FROM inserted i
    INNER JOIN deleted d ON i.RequestId = d.RequestId
    WHERE ISNULL(i.CurrentStatusId, -1) <> ISNULL(d.CurrentStatusId, -1);
END;
GO
