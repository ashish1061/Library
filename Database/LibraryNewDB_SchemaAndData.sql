USE [master]
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LibraryNewDB')
BEGIN
    CREATE DATABASE [LibraryNewDB]
     CONTAINMENT = NONE
     ON  PRIMARY 
    ( NAME = N'LibraryNewDB', FILENAME = N'D:\SQL\MSSQL12.MSSQLSERVER\MSSQL\DATA\LibraryNewDB.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
     LOG ON 
    ( NAME = N'LibraryNewDB_log', FILENAME = N'D:\SQL\MSSQL12.MSSQLSERVER\MSSQL\DATA\LibraryNewDB_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
     WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
END
GO

ALTER DATABASE [LibraryNewDB] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [LibraryNewDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [LibraryNewDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [LibraryNewDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [LibraryNewDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [LibraryNewDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [LibraryNewDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [LibraryNewDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [LibraryNewDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [LibraryNewDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [LibraryNewDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [LibraryNewDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [LibraryNewDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [LibraryNewDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [LibraryNewDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [LibraryNewDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [LibraryNewDB] SET  ENABLE_BROKER 
GO
ALTER DATABASE [LibraryNewDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [LibraryNewDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [LibraryNewDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [LibraryNewDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [LibraryNewDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [LibraryNewDB] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [LibraryNewDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [LibraryNewDB] SET RECOVERY FULL 
GO
ALTER DATABASE [LibraryNewDB] SET  MULTI_USER 
GO
ALTER DATABASE [LibraryNewDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [LibraryNewDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [LibraryNewDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [LibraryNewDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [LibraryNewDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [LibraryNewDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'LibraryNewDB', N'ON'
GO
ALTER DATABASE [LibraryNewDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [LibraryNewDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [LibraryNewDB]
GO

-- Drop tables if they exist so we can recreate them
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Books', 'U') IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID('dbo.BookCategories', 'U') IS NOT NULL DROP TABLE dbo.BookCategories;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL DROP TABLE dbo.__EFMigrationsHistory;
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BookCategories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Category] [nvarchar](max) NOT NULL,
	[SubCategory] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_BookCategories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Books](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](max) NOT NULL,
	[Author] [nvarchar](max) NOT NULL,
	[Price] [real] NOT NULL,
	[Ordered] [bit] NOT NULL,
	[BookCategoryId] [int] NOT NULL,
 CONSTRAINT [PK_Books] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Orders](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[BookId] [int] NOT NULL,
	[OrderDate] [datetime2](7) NOT NULL,
	[Returned] [bit] NOT NULL,
	[ReturnDate] [datetime2](7) NULL,
	[FinePaid] [int] NOT NULL,
 CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](max) NOT NULL,
	[LastName] [nvarchar](max) NOT NULL,
	[Email] [nvarchar](max) NOT NULL,
	[Password] [nvarchar](max) NOT NULL,
	[MobileNumber] [nvarchar](max) NOT NULL,
	[CreatedOn] [datetime2](7) NOT NULL,
	[UserType] [nvarchar](max) NOT NULL,
	[AccountStatus] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20231118143947_initialdb', N'8.0.10')
GO
SET IDENTITY_INSERT [dbo].[BookCategories] ON 
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (1, N'computer', N'algorithm')
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (2, N'computer', N'programming languages')
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (3, N'computer', N'networking')
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (4, N'computer', N'hardware')
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (5, N'mechanical', N'machine')
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (6, N'mechanical', N'transfer of energy')
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (7, N'mathematics', N'calculus')
GO
INSERT [dbo].[BookCategories] ([Id], [Category], [SubCategory]) VALUES (8, N'mathematics', N'algebra')
GO
SET IDENTITY_INSERT [dbo].[BookCategories] OFF
GO
SET IDENTITY_INSERT [dbo].[Books] ON 
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (1, N'Introduction to Algorithm', N'Thomas Corman', 100, 1, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (2, N'Introduction to Algorithm', N'Thomas Corman', 100, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (3, N'Algorithms', N'Robert Sedgewick & Kevin Wayne', 200, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (4, N'The Algorithm Design Manual', N'Steve Skiena', 300, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (5, N'Algorithms For Interviews', N'Adnan Aziz', 400, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (6, N'Algorithms For Interviews', N'Adnan Aziz', 400, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (7, N'Algorithms For Interviews', N'Adnan Aziz', 400, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (8, N'Algorithm in Nutshell', N'George Heineman', 500, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (9, N'Klienberg & Tardos', N'Algorithm Design', 600, 0, 1)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (10, N'Python Crash Course: A Hands-On, Project-Based Introduction to Programming', N'Eric Matthes', 700, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (11, N'Python Crash Course: A Hands-On, Project-Based Introduction to Programming', N'Eric Matthes', 700, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (12, N'Python Crash Course: A Hands-On, Project-Based Introduction to Programming', N'Eric Matthes', 700, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (13, N'Head First Python: A Brain-Friendly Guide', N'Paul Barry', 800, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (14, N'Effective Java', N'Joshua Bloch', 900, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (15, N'Effective Java', N'Joshua Bloch', 900, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (16, N'Head First Java', N'Kathy Sierra and Bert Bates', 1000, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (17, N'C Programming Language', N'Brian W. Kernighan, Dennis M. Ritchie', 1100, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (18, N'C Programming Language', N'Brian W. Kernighan, Dennis M. Ritchie', 1100, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (19, N'C Programming Language', N'Brian W. Kernighan, Dennis M. Ritchie', 1100, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (20, N'Eloquent JavaScript: A Modern Introduction to Programming', N'Marijn Haverbeke', 1200, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (21, N'The Art of Computer Programming', N'Donald E. Knuth', 1300, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (22, N'The Art of Computer Programming', N'Donald E. Knuth', 1300, 0, 2)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (23, N'A Top-Down Approach: Computer Networking', N'James F Kurose and Keith W Ross', 1400, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (24, N'The All-New Switch Book (2nd Edition)', N'Rich Seifert and James Edwards', 1500, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (25, N'The All-New Switch Book (2nd Edition)', N'Rich Seifert and James Edwards', 1500, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (26, N'Business Data Communications and Networking (14th Edition)', N'Jerry FitzGerald, Alan Dennis, and Alexandra Durcikova', 1600, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (27, N'Data Communications and Networking with TCP/IP Protocol Suite, 6th Edition', N'Forouzan', 1700, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (28, N'Network Warrior, 2nd Edition', N'Gary Donahue', 1800, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (29, N'Network Warrior, 2nd Edition', N'Gary Donahue', 1800, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (30, N'Network Warrior, 2nd Edition', N'Gary Donahue', 1800, 0, 3)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (31, N'Microprocessor Architecture, Programming, and Applications with the 8085 (4th Edition)', N'Ramesh Gaonkar', 1900, 0, 4)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (32, N'Microprocessors and Interfacing: Programming and Hardware (Hardcover)', N'Douglas V. Hall', 2000, 0, 4)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (33, N'Microprocessors and Interfacing: Programming and Hardware (Hardcover)', N'Douglas V. Hall', 2000, 0, 4)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (34, N'Embedded Microprocessor Systems Design', N'Kenneth L. Short', 2100, 0, 4)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (35, N'Digital Electronics & Microprocessor', N'Dr. Vibhav Kumar Sachan', 2200, 0, 4)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (36, N'Real-Time Embedded Systems', N'Xiaocong Fan', 2300, 0, 4)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (37, N'Digital Interface Design and Application', N'Jonathan A. Dell', 2400, 0, 4)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (38, N'Richard G. Budynas and Keith J. Nisbett', N'Shigley''s Mechanical Engineering Design', 2500, 0, 5)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (39, N'Richard G. Budynas and Keith J. Nisbett', N'Shigley''s Mechanical Engineering Design', 2500, 0, 5)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (40, N'Richard G. Budynas and Keith J. Nisbett', N'Shigley''s Mechanical Engineering Design', 2500, 0, 5)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (41, N'Machinery''s Handbook', N'Erik Oberg', 2600, 0, 5)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (42, N'Introduction to Robotics: Mechanics and Control', N'John J. Craig', 2700, 0, 5)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (43, N'Machine Design', N'Robert L. Norton', 2800, 0, 5)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (44, N'Machine Design', N'Robert L. Norton', 2800, 0, 5)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (45, N'Fluid Mechanics', N'Frank M. White', 3000, 1, 6)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (46, N'Fundamentals of Thermodynamics', N'Claus Borgnakke and Richard E. Sonntag', 3100, 0, 6)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (47, N'Fundamentals of Thermodynamics', N'Claus Borgnakke and Richard E. Sonntag', 3100, 0, 6)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (48, N'Calculus: Early Transcendentals', N'James Stewart', 3200, 0, 7)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (49, N'Calculus for Dummies', N'Mark Ryan', 3300, 0, 7)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (50, N'Calculus for Dummies', N'Mark Ryan', 3300, 0, 7)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (51, N'The Calculus with Analytic Geometry', N'Louis Leithold', 3400, 0, 7)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (52, N'Euclid''s Elements', N'Euclid', 3500, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (53, N'The Man Who Knew Infinity: A Life of the Genius Ramanujan', N'Robert Kanigel', 3600, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (54, N'The Man Who Knew Infinity: A Life of the Genius Ramanujan', N'Robert Kanigel', 3600, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (55, N'A Brief History of Time', N'Stephen Hawking', 3700, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (56, N'Relativity: The Special and the General Theory', N'Albert Einstein', 3800, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (57, N'Relativity: The Special and the General Theory', N'Albert Einstein', 3800, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (58, N'Relativity: The Special and the General Theory', N'Albert Einstein', 3800, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (59, N'Relativity: The Special and the General Theory', N'Albert Einstein', 3800, 0, 8)
GO
INSERT [dbo].[Books] ([Id], [Title], [Author], [Price], [Ordered], [BookCategoryId]) VALUES (60, N'Relativity: The Special and the General Theory', N'Albert Einstein', 3800, 0, 8)
GO
SET IDENTITY_INSERT [dbo].[Books] OFF
GO
SET IDENTITY_INSERT [dbo].[Orders] ON 
GO
INSERT [dbo].[Orders] ([Id], [UserId], [BookId], [OrderDate], [Returned], [ReturnDate], [FinePaid]) VALUES (1, 1, 1, CAST(N'2025-10-16T12:29:13.1907424' AS DateTime2), 0, NULL, 0)
GO
INSERT [dbo].[Orders] ([Id], [UserId], [BookId], [OrderDate], [Returned], [ReturnDate], [FinePaid]) VALUES (2, 1, 45, CAST(N'2025-10-16T12:29:35.9079374' AS DateTime2), 0, NULL, 0)
GO
SET IDENTITY_INSERT [dbo].[Orders] OFF
GO
SET IDENTITY_INSERT [dbo].[Users] ON 
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [Email], [Password], [MobileNumber], [CreatedOn], [UserType], [AccountStatus]) VALUES (1, N'Admin', N'', N'admin@gmail.com', N'admin1999', N'1234567890', CAST(N'2023-11-01T13:28:12.0000000' AS DateTime2), N'ADMIN', N'ACTIVE')
GO
SET IDENTITY_INSERT [dbo].[Users] OFF
GO
CREATE NONCLUSTERED INDEX [IX_Books_BookCategoryId] ON [dbo].[Books]
(
	[BookCategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Orders_BookId] ON [dbo].[Orders]
(
	[BookId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Orders_UserId] ON [dbo].[Orders]
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Books]  WITH CHECK ADD  CONSTRAINT [FK_Books_BookCategories_BookCategoryId] FOREIGN KEY([BookCategoryId])
REFERENCES [dbo].[BookCategories] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Books] CHECK CONSTRAINT [FK_Books_BookCategories_BookCategoryId]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [FK_Orders_Books_BookId] FOREIGN KEY([BookId])
REFERENCES [dbo].[Books] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [FK_Orders_Books_BookId]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [FK_Orders_Users_UserId]
GO
USE [master]
GO
ALTER DATABASE [LibraryNewDB] SET  READ_WRITE 
GO
