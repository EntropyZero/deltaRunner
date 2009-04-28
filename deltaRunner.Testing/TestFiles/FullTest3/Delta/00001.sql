if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[DELTA_ColumnAdder]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[DELTA_ColumnAdder]
GO

create proc DELTA_ColumnAdder
(
	@tableName		varchar(255)
	, @columnName	varchar(255)
	, @addSQL		varchar(512)
)
AS
BEGIN

print 'Add Column [' + @columnName + '] to Table [' + @tableName + ']'

IF NOT EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name=@tableName
                        AND 
                        C.Name=@columnName
        )
        BEGIN
			PRINT ' -- Adding Column'

 			EXEC(N'ALTER TABLE [dbo].[' + @tableName + '] ADD ' + @addSQL)
 					
		END
END
go

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[DELTA_ColumnRemover]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[DELTA_ColumnRemover]
GO

create proc DELTA_ColumnRemover
(
	@tableName		varchar(255)
	, @columnName	varchar(255)
)
AS
BEGIN

print 'Remove Column [' + @columnName + '] from Table [' + @tableName + ']'

IF EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name=@tableName
                        AND 
                        C.Name=@columnName
        )
        BEGIN
			PRINT ' -- Removing Column'

 			EXEC(N'ALTER TABLE [dbo].[' + @tableName + '] DROP COLUMN ' + @columnName)
 					
END
END
go