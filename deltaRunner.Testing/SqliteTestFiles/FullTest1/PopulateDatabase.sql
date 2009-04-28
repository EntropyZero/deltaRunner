if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Book]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[Book]
GO

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[Author]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
drop table [dbo].[Author]
GO

CREATE TABLE [Author]
(
	[AuthorId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY
	, [Lastname] VARCHAR(55) NOT NULL
	, [Firstname] VARCHAR(25) NOT NULL
)
GO

CREATE TABLE [Book]
(
	[BookId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY
	, [AuthorId] INT NOT NULL CONSTRAINT fk_Book_Author_AuthorId FOREIGN KEY ([AuthorId]) REFERENCES [Author]([AuthorId])
	, [Title] VARCHAR(55) NOT NULL
)
GO

PRINT CAST(CURRENT_TIMESTAMP AS VARCHAR(30)) + ': Inserting Author'
SET IDENTITY_INSERT [dbo].[Author] ON
INSERT INTO [Author]
	(
	[AuthorId]
	, [Lastname] 
	, [Firstname]
	)
	SELECT 1, 'Brockey', 'Michael' UNION
	SELECT 2, 'Buxton', 'Stevem' UNION
	SELECT 3, 'Jones', 'Happy' UNION
	SELECT 4, 'Smith', 'john' UNION
	SELECT 5, 'Johnson', 'Todd' UNION
	SELECT 6, 'Brockey', 'Michael' UNION
	SELECT 7, 'Buxton', 'Stevem' UNION
	SELECT 8, 'Jones', 'Happy' UNION
	SELECT 9, 'Smith', 'john' UNION
	SELECT 10, 'Johnson', 'Todd'
	
SET IDENTITY_INSERT [dbo].[Author] OFF
GO

PRINT CAST(CURRENT_TIMESTAMP AS VARCHAR(30)) + ': Inserting Book'
SET IDENTITY_INSERT [dbo].[Book] ON
INSERT INTO [Book]
	(
	[BookId]
	, [AuthorId]
	, [Title]
	)
	SELECT 1, 1, 'My First Book' UNION
	SELECT 2, 1, 'My Second Book' UNION
	SELECT 3, 2, 'My First Book' UNION
	SELECT 4, 2, 'My Second Book' UNION
	SELECT 5, 3, 'My First Book' UNION
	SELECT 6, 4, 'My First Book' UNION
	SELECT 7, 5, 'My First Book' UNION
	SELECT 8, 2, 'My Second Book' UNION
	SELECT 9, 3, 'My First Book' UNION
	SELECT 10, 4, 'My First Book'
	
SET IDENTITY_INSERT [dbo].[Book] OFF
GO

