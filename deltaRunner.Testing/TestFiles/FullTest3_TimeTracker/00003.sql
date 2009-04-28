
if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[TT_ListTimeEntries]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[TT_ListTimeEntries]
GO

CREATE PROCEDURE [dbo].[TT_ListTimeEntries]
(
	@QueryUserID int,
	@UserID int,
	@StartDate datetime,
	@EndDate datetime,
	@ProjectIDList nvarchar(512)
)
AS
	SET @ProjectIDList = NULLIF(@ProjectIDList, CAST(0 AS VARCHAR))
	SET @ProjectIDList = NULLIF(@ProjectIDList, CAST('' AS VARCHAR))
	
	CREATE TABLE #ProjectIds (ProjectId int)
	DECLARE @sSqlString nvarchar(1024)
	IF(@ProjectIDList IS NULL)
		SET @sSqlString = 'INSERT INTO #ProjectIds(ProjectID) select distinct ProjectID from TT_EntryLog'
	ELSE
		SET @sSqlString = 'INSERT INTO #ProjectIds(ProjectID) select distinct ProjectID from TT_EntryLog where ProjectID in (' + @ProjectIDList + ')'
		
	EXEC sp_executesql @sSqlString

	DECLARE @@QueryUserRoleID int

	SELECT @@QueryUserRoleID = TT_Users.RoleID FROM TT_Users WHERE TT_Users.UserID = @QueryUserID

	IF @@QueryUserRoleID = 1 or @QueryUserID = @UserID
		BEGIN
			SELECT 
				EntryLogID, TT_EntryLog.Description, Duration, EntryDate, TT_EntryLog.ProjectID AS ProjectID, 
				TT_EntryLog.CategoryID AS CategoryID, TT_Categories.Abbreviation AS CategoryName, TT_Projects.Name AS ProjectName,
				ManagerUserID, TT_Categories.Name AS CatShortName
			FROM 
				TT_EntryLog 
					INNER JOIN 
					TT_Categories 
					ON 
					TT_EntryLog.CategoryID = TT_Categories.CategoryID 
					INNER JOIN 
					TT_Projects 
					ON 
					TT_EntryLog.ProjectID = TT_Projects.ProjectID	
			WHERE 
				UserID = @UserID 
				AND 
				EntryDate >= @StartDate
				AND 
				EntryDate <= @EndDate
				AND
				TT_EntryLog.ProjectID IN (SELECT ProjectId FROM #ProjectIds)
				
		END
	ELSE IF @@QueryUserRoleID = 2
		BEGIN
			SELECT 
				EntryLogID, TT_EntryLog.Description, Duration, EntryDate, TT_EntryLog.ProjectID AS ProjectID, 
				TT_EntryLog.CategoryID AS CategoryID, TT_Categories.Abbreviation AS CategoryName, TT_Projects.Name AS ProjectName,
				ManagerUserID, TT_Categories.Name AS CatShortName
			FROM 
				TT_EntryLog 
					INNER JOIN 
					TT_Categories 
					ON 
					TT_EntryLog.CategoryID = TT_Categories.CategoryID 
					INNER JOIN 
					TT_Projects 
					ON 
					TT_EntryLog.ProjectID = TT_Projects.ProjectID	
			WHERE 
				UserID = @UserID 
				AND 
				Convert(nvarchar, EntryDate, 1) >= Convert(nvarchar, @StartDate, 1)
				AND 
				Convert(nvarchar, EntryDate, 1) <= Convert(nvarchar, @EndDate, 1)
				AND
				ManagerUserID = @QueryUserID
		END
GO