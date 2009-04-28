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
GO

-- EXEC DELTA_ColumnAdder @tableName = '', @columnName = '', @addSQL = '' 

EXEC DELTA_ColumnAdder @tableName = 'TT_Users', @columnName = 'Disabled', @addSQL = 'Disabled BIT NOT NULL DEFAULT(0)' 

if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[TT_UserLogin]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[TT_UserLogin]
GO

CREATE Procedure [TT_UserLogin]
    (
        @UserName  nvarchar(100),
        @Password nvarchar(50)
    )
    AS

    SELECT
        UserName

    FROM
        TT_Users

    WHERE
        UserName = @UserName
    AND
        Password = @Password
    AND
		Disabled = 0
GO