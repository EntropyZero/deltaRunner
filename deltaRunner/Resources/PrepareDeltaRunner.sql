
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[{0}]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
	CREATE TABLE [dbo].[{0}]
	(
		[{1}] int
	)
GO

IF NOT EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name='{0}' 
                        AND 
                        C.Name='Hash' 
        )
        BEGIN
 			ALTER TABLE [dbo].[{0}] ADD Hash VARCHAR(50)
		END
GO

IF NOT EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name='{0}' 
                        AND 
                        C.Name='Filename' 
        )
        BEGIN
 			ALTER TABLE [dbo].[{0}] ADD Filename VARCHAR(255)
		END
GO

IF NOT EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID INNER JOIN SysTypes st on c.xtype = st.xtype
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name='{0}' 
                        AND 
                        C.Name='{1}' 
                        AND
						st.[name]='varchar'
        )
        BEGIN
 			ALTER TABLE [dbo].[{0}] ADD tmpId VARCHAR(25)
		END
GO

IF EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID INNER JOIN SysTypes st on c.xtype = st.xtype
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name='{0}' 
                        AND 
                        C.Name='tmpId' 
        )
        BEGIN
 			EXEC sp_executesql N'UPDATE [dbo].[{0}] SET tmpId = CASE when filename is null then ''0'' else LEFT(Filename, LEN(Filename) - 4) END'
 			EXEC sp_executesql N'UPDATE [dbo].[{0}] SET tmpId = {1} WHERE {1} IN (-1,-2)'  			
 			EXEC sp_executesql N'ALTER TABLE [dbo].[{0}] DROP COLUMN [{1}]'
		END
GO

IF EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID INNER JOIN SysTypes st on c.xtype = st.xtype
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name='{0}' 
                        AND 
                        C.Name='tmpId' 
        )
        BEGIN
 			EXEC sp_rename '[{0}].[tmpId]', '{1}', 'COLUMN'				 			
 			
 			IF EXISTS
			(
					SELECT * FROM SysObjects O
					WHERE 
							ObjectProperty(O.ID,'IsUserTable')=1 
							AND 
							O.Name='df_ChangeTracking' 
			)
			BEGIN
 				DELETE FROM df_ChangeTracking WHERE TableName = '{0}';
			END
		END
GO

IF NOT EXISTS
        (
                SELECT * FROM SysObjects O INNER JOIN SysColumns C ON O.ID=C.ID
                WHERE 
                        ObjectProperty(O.ID,'IsUserTable')=1 
                        AND 
                        O.Name='{0}' 
                        AND 
                        C.Name='DateRun' 
        )
        BEGIN
 			ALTER TABLE [dbo].[{0}] ADD DateRun DateTime Default(GetDate())
		END
GO