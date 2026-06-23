-- Add IsDeleted to Books and Employee
IF COL_LENGTH('Books', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Books ADD IsDeleted BIT NOT NULL DEFAULT 0;
END
IF COL_LENGTH('Employee', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Employee ADD IsDeleted BIT NOT NULL DEFAULT 0;
END
GO

-- Create CDC Trigger for Books
IF OBJECT_ID('trg_Audit_Books_Update', 'TR') IS NOT NULL
    DROP TRIGGER trg_Audit_Books_Update;
GO
CREATE TRIGGER trg_Audit_Books_Update
ON Books
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO AuditLogs (Action, EmpID, Entity, EntityId, Details)
    SELECT 
        'Update',
        'SYSTEM',
        'Book',
        CAST(i.Anum AS NVARCHAR(50)),
        'Book details updated'
    FROM inserted i;
END
GO

-- Create CDC Trigger for Employee
IF OBJECT_ID('trg_Audit_Employee_Update', 'TR') IS NOT NULL
    DROP TRIGGER trg_Audit_Employee_Update;
GO
CREATE TRIGGER trg_Audit_Employee_Update
ON Employee
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO AuditLogs (Action, EmpID, Entity, EntityId, Details)
    SELECT 
        'Update',
        i.EmpID,
        'Employee',
        i.EmpID,
        'Employee details updated'
    FROM inserted i;
END
GO
