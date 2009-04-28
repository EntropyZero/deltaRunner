drop table if exists [Book]
GO

drop table if exists [Author]
GO

CREATE TABLE [Author]
(
	[AuthorId] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
	, [Lastname] VARCHAR(55) NOT NULL
	, [Firstname] VARCHAR(25) NOT NULL
)
GO

CREATE TABLE [Book]
(
	[BookId] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
	, [AuthorId] INT NOT NULL 
	, [Title] VARCHAR(55) NOT NULL
)
GO


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
	

GO


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

GO

