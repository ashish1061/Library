USE Library;
GO

-- Create OtpStore table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OtpStore]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OtpStore](
        [Email] [nvarchar](256) NOT NULL,
        [OtpCode] [nvarchar](10) NOT NULL,
        [Expiry] [datetime] NOT NULL,
        [CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_OtpStore_CreatedAt] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_OtpStore] PRIMARY KEY CLUSTERED ([Email] ASC)
    );
    PRINT 'Created OtpStore table.';
END
ELSE
BEGIN
    PRINT 'OtpStore table already exists.';
END
GO

-- Create UserRefreshTokens table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRefreshTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserRefreshTokens](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [EmpID] [nvarchar](50) NOT NULL,
        [Token] [nvarchar](256) NOT NULL,
        [Expiry] [datetime] NOT NULL,
        [IsRevoked] [bit] NOT NULL CONSTRAINT [DF_UserRefreshTokens_IsRevoked] DEFAULT (0),
        [CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_UserRefreshTokens_CreatedAt] DEFAULT (GETUTCDATE()),
        [RevokedAt] [datetime] NULL,
        [ReplacedByToken] [nvarchar](256) NULL,
        CONSTRAINT [PK_UserRefreshTokens] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created UserRefreshTokens table.';
END
ELSE
BEGIN
    PRINT 'UserRefreshTokens table already exists.';
END
GO

-- Create non-clustered index on Token in UserRefreshTokens
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserRefreshTokens_Token' AND object_id = OBJECT_ID('[dbo].[UserRefreshTokens]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserRefreshTokens_Token] ON [dbo].[UserRefreshTokens] ([Token] ASC);
    PRINT 'Created index IX_UserRefreshTokens_Token.';
END
GO

-- Create non-clustered index on EmpID in UserRefreshTokens
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserRefreshTokens_EmpID' AND object_id = OBJECT_ID('[dbo].[UserRefreshTokens]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserRefreshTokens_EmpID] ON [dbo].[UserRefreshTokens] ([EmpID] ASC);
    PRINT 'Created index IX_UserRefreshTokens_EmpID.';
END
GO
