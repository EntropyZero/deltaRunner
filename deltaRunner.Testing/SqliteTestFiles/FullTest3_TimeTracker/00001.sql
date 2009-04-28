if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[TT_ListUserTimeSummary]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure [dbo].[TT_ListUserTimeSummary]
GO

CREATE  PROCEDURE [dbo].[TT_ListUserTimeSummary]
(
    @ManagerUserID int,
    @UserIDList nvarchar(512),
    @ProjectIDList nvarchar(512),
    @StartDate datetime,
    @EndDate datetime
)
AS

	SET @ProjectIDList = NULLIF(@ProjectIDList, CAST(0 AS VARCHAR))
	SET @ProjectIDList = NULLIF(@ProjectIDList, CAST('' AS VARCHAR))
	
	DECLARE 
		@sSqlString nvarchar(1024),
		@sSubSql nvarchar(1024),
		@RoleID int
	    
	SELECT @RoleID = RoleID
	FROM TT_Users 
	WHERE UserID = @ManagerUserID;

	IF (@RoleID = 1)
		BEGIN
			SET @sSqlString = 'SELECT Sum(EL.Duration) TotalHours, U.UserID, U.UserName'
			SET @sSqlString = @sSqlString + ' FROM TT_EntryLog EL Inner Join TT_Users U On EL.UserID = U.UserID WHERE U.UserID IN (' + @UserIDList + ')'
			IF (@ProjectIDList IS NOT NULL)
				BEGIN
					SET @sSqlString = @sSqlString + ' AND EL.ProjectID IN (' + @ProjectIDList + ')' 
				END
			SET @sSqlString = @sSqlString + ' and EL.EntryDate >= @1 and EL.EntryDate <= @2  GROUP BY U.UserID, U.UserName'
		END
		
	ELSE IF (@RoleID = 2)
		BEGIN   
			SET @sSubSql = 'SELECT PM.UserID FROM TT_Projects P INNER JOIN TT_ProjectMembers PM'
			SET @sSubSql = @sSubSql + ' ON P.ProjectID = PM.ProjectID WHERE P.ManagerUserID = @3 AND PM.UserID IN (' + @UserIDList + ')' 
		    
			SET @sSqlString = 'SELECT Sum(EL.Duration) TotalHours, U.UserID, U.UserName'
			SET @sSqlString = @sSqlString + ' FROM TT_EntryLog EL Inner Join TT_Users U On EL.UserID = U.UserID WHERE U.UserID IN (' + @sSubSql + ')'
			SET @sSqlString = @sSqlString + ' AND EL.ProjectID IN (SELECT ProjectID From TT_Projects Where ManagerUserID = @3) '
			IF (@ProjectIDList IS NOT NULL)
				BEGIN
					SET @sSqlString = @sSqlString + ' AND EL.ProjectID IN (' + @ProjectIDList + ')' 
				END
			SET @sSqlString = @sSqlString + ' and EL.EntryDate >= @1 and EL.EntryDate <= @2  GROUP BY U.UserID, U.UserName'
		END
	ELSE 
		SET @sSqlString = 'SELECT U.UserID AS TotalHours, U.UserID, U.UserName From TT_Users U Where 1=0'
		
	EXEC sp_executesql @sSqlString, N'@1 datetime, @2 datetime, @3 int', @StartDate, @EndDate, @ManagerUserID
	
GO

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
				Convert(nvarchar, EntryDate, 1) >= Convert(nvarchar, @StartDate, 1)
				AND 
				Convert(nvarchar, EntryDate, 1) <= Convert(nvarchar, @EndDate, 1)
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