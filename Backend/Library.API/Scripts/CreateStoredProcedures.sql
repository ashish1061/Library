USE [library]
GO

-- User Management
CREATE OR ALTER PROCEDURE sp_ValidateUser
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255)
AS
BEGIN
    -- Dummy implementation, adjust according to actual table structure
    SELECT Id, Username, Role 
    FROM Users 
    WHERE Username = @Username AND PasswordHash = @PasswordHash
END
GO

CREATE OR ALTER PROCEDURE sp_CreateUser
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(255),
    @Role NVARCHAR(20)
AS
BEGIN
    INSERT INTO Users (Username, PasswordHash, Role)
    VALUES (@Username, @PasswordHash, @Role)
END
GO

-- Issues
CREATE OR ALTER PROCEDURE sp_GetIssues
AS
BEGIN
    SELECT IssueNumber, Anum, BookName, EmpID, EmpName, IssueDate 
    FROM Issue
END
GO

CREATE OR ALTER PROCEDURE sp_CreateIssue
    @Anum BIGINT,
    @BookName NVARCHAR(MAX),
    @EmpID NVARCHAR(MAX),
    @EmpName NVARCHAR(MAX),
    @IssueDate NVARCHAR(MAX)
AS
BEGIN
    INSERT INTO Issue (Anum, BookName, EmpID, EmpName, IssueDate)
    VALUES (@Anum, @BookName, @EmpID, @EmpName, @IssueDate)
END
GO

-- Add other SPs as needed for Books, Employees, Magazines, etc.
