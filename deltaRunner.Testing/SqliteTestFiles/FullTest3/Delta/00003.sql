ALTER TABLE Author RENAME TO AuthorTmp
GO

CREATE TABLE [Author]
(
	[AuthorId] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
	, [Lastname] VARCHAR(55) NOT NULL
	, [Firstname] VARCHAR(25) NOT NULL
	, [Hometown] VARCHAR(50) NULL
)
GO

Insert into Author(AuthorId, LastName, FirstName, Hometown) Select AuthorID, Lastname, FirstName, NULL From AuthorTmp
GO

Drop Table AuthorTmp
GO