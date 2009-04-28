ALTER TABLE Book Rename To BookTmp
GO

CREATE TABLE [Book]
(
	[BookId] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
	, [AuthorId] INT NOT NULL 
	, [Title] VARCHAR(55) NOT NULL
	, [YearPublished] int null
)
GO

Insert Into [Book] (BookId, AuthorId, Title, YearPublished) Select BookId, AuthorId, Title, NULL from BookTmp
GO

Drop Table BookTmp
GO