-- Create New Database Script
CREATE DATABASE [LibraryNewDB]
GO

USE [LibraryNewDB]
GO

-- Create Users Table
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](50) NOT NULL,
	[PasswordHash] [nvarchar](255) NOT NULL,
	[Role] [nvarchar](20) NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)
)
GO

-- Create Issues Table
CREATE TABLE [dbo].[Issue](
	[IssueNumber] [int] IDENTITY(1,1) NOT NULL,
	[Anum] [bigint] NOT NULL,
	[BookName] [nvarchar](max) NOT NULL,
	[EmpID] [nvarchar](max) NOT NULL,
	[EmpName] [nvarchar](max) NOT NULL,
	[IssueDate] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Issue] PRIMARY KEY CLUSTERED 
(
	[IssueNumber] ASC
)
)
GO

-- Create Books Table
CREATE TABLE [dbo].[Books](
	[Anum] [bigint] NOT NULL,
	[BookName] [nvarchar](max) NOT NULL,
	[Author] [nvarchar](max) NULL,
	[Publisher] [nvarchar](max) NULL,
	[Quantity] [int] NOT NULL,
 CONSTRAINT [PK_Books] PRIMARY KEY CLUSTERED 
(
	[Anum] ASC
)
)
GO

-- Create Employees Table
CREATE TABLE [dbo].[Employees](
	[EmpID] [nvarchar](50) NOT NULL,
	[EmpName] [nvarchar](max) NOT NULL,
	[Department] [nvarchar](max) NULL,
	[Email] [nvarchar](max) NULL,
 CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED 
(
	[EmpID] ASC
)
)
GO
