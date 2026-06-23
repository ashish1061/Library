USE Library;
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('Employee') AND name = 'FailedLoginAttempts'
)
BEGIN
    ALTER TABLE Employee ADD FailedLoginAttempts INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('Employee') AND name = 'LockoutEnd'
)
BEGIN
    ALTER TABLE Employee ADD LockoutEnd DATETIME NULL;
END
GO
