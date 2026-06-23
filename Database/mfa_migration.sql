USE [Library]
GO

-- 1. Add IsMfaEnabled column if it does not exist
IF COL_LENGTH('Employee', 'IsMfaEnabled') IS NULL
BEGIN
    ALTER TABLE Employee ADD IsMfaEnabled BIT NOT NULL DEFAULT 1;
    PRINT 'Added IsMfaEnabled column to Employee table.';
END
ELSE
BEGIN
    PRINT 'IsMfaEnabled column already exists in Employee table.';
END
GO

-- 2. Create index on emailid if it doesn''t exist to optimize queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Employee_EmailId' AND object_id = OBJECT_ID('Employee'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Employee_EmailId ON Employee (emailid);
    PRINT 'Created index IX_Employee_EmailId on Employee table.';
END
GO
