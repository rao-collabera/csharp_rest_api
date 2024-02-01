-- The OPENJSON function is available only under compatibility level 130 or higher. 
-- You can change the compatibility level of your database, If it is lower than 130
-- ALTER DATABASE database_name SET COMPATIBILITY_LEVEL = 130
------------------------------------------------------------------------------------------------------------------------------
--*******************		Tables		*******************
------------------------------------------------------------------------------------------------------------------------------
CREATE TABLE [dbo].[activity_types](
	[activity_type_Id] [int] IDENTITY(1,1) NOT NULL,
	[activity_type] [varchar](50) NOT NULL,
	CONSTRAINT [PK_activity_types] PRIMARY KEY CLUSTERED ( [activity_type_Id] ASC )
) 
GO

CREATE TABLE [dbo].[activities](
	[activity_id] [int] IDENTITY(1,1) NOT NULL,
	[activity_name] [varchar](50) NOT NULL,
	[activity_date] [datetime] NOT NULL,
	[description] [varchar](3500) NOT NULL,
	[crtd_user] [varchar](50) NOT NULL,
	[crtd_datetime] [datetime] NOT NULL,
	[activity_type_Id] [int] NOT NULL,
	CONSTRAINT [PK_activities] PRIMARY KEY CLUSTERED ( [activity_id] ASC), 
	CONSTRAINT [FK_activity_types] FOREIGN KEY (activity_type_Id)
    REFERENCES dbo.activity_types (activity_type_Id)	
) 
GO

CREATE TABLE [dbo].[Json_SPs](
	[Action_Name] [nvarchar](50) NOT NULL,
	[Action_Type] [nvarchar](50) NOT NULL,
	[SP_Name] [nvarchar](50) NOT NULL,
	[Json_Schema] [nvarchar](1500) NOT NULL,
	[Json] [nvarchar](500) NULL,
 CONSTRAINT [PK_Json_SPs] PRIMARY KEY CLUSTERED ( [Action_Name] ASC ),
 INDEX [IX_Json_SPs] NONCLUSTERED ( [SP_Name] ASC )
) 
GO
------------------------------------------------------------------------------------------------------------------------------
--*******************		Get Activities		*******************
------------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[jsp_GetActivities](@UserId VARCHAR(50), @Json NVARCHAR(MAX)) AS 
BEGIN
	DECLARE @Stmt NVARCHAR(MAX),  @SearchCriteria VARCHAR(50) = '', @sortColumn VARCHAR(50) = '', @sortDirection VARCHAR(10) = '', @PageNo VARCHAR(4), @PageSize VARCHAR(4), @Activity_Types VARCHAR(500) = ''

	IF TRIM(@UserId) <> '' AND ISJSON(@Json) > 0
	BEGIN
		SELECT @Json = REPLACE(@Json, ColumnName, ColumnName) FROM (VALUES ('"searchcriteria"'), ('"sortcolumn"'), ('"sortdirection"'), ('"pageno"'), ('"pagesize"')) AS X(ColumnName)

		SELECT @SearchCriteria = searchCriteria, @sortColumn = sortColumn, @sortDirection = sortDirection, @PageNo = CONVERT(VARCHAR, pageNo), @PageSize = CONVERT(VARCHAR, pageSize)
		FROM OPENJSON(@Json) WITH (searchCriteria NVARCHAR(50) '$.searchcriteria', sortColumn NVARCHAR(50) '$.sortcolumn', sortDirection NVARCHAR(10) '$.sortdirection', pageNo INT '$.pageno', pageSize INT '$.pagesize')
		
		SELECT @Activity_Types =  JSON_QUERY(@Json, '$.activity_types')
		IF @Activity_Types <> ''
		BEGIN
			SET @Activity_Types = REPLACE(@Activity_Types, '''', '''''')
			SET @Activity_Types = REPLACE(@Activity_Types, '"', '''')
			SET @Activity_Types = ' AND activity_type  IN (' + SUBSTRING(@Activity_Types, 2, LEN(@Activity_Types) - 2) + ')'
		END
		ELSE
		SET @Activity_Types = ''

		IF @sortColumn IS NULL OR @sortColumn = ''
		SET @sortColumn = 'activity_id desc'
		ELSE 
		SET @sortColumn = @sortColumn + ' ' + COALESCE(@sortDirection, '')

		IF @SearchCriteria IS NULL
		SET @SearchCriteria = ''
		ELSE
		SET @SearchCriteria = REPLACE(@SearchCriteria, '''', '''''')

		IF @PageNo IS NULL
		SET @PageNo = '1'

		IF @PageSize IS NULL
		SET @PageSize = '20'

		IF @sortColumn = '' OR @PageNo = '0' OR @PageSize = '0' OR LEN(@PageNo) > 3 OR LEN(@PageSize) > 3
			SET @Stmt = 'SELECT (SELECT 200 AS statusCode, '''' AS errorMessage, 0 AS [data.totalRecords] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result'
		ELSE
		BEGIN
			SET @PageNo = CONVERT(VARCHAR, CONVERT(INT, @PageNo) - 1)

			SET @Stmt = ';WITH Results AS (SELECT a.activity_id, a.activity_name, t.activity_type, a.activity_date, a.description
			FROM dbo.activities AS a WITH (NOLOCK) 
			INNER JOIN dbo.activity_types AS t WITH (NOLOCK) ON a.activity_type_Id = t.activity_type_Id
			WHERE a.crtd_user = ''' + @UserId + ''''
			IF @SearchCriteria = ''
				SET @Stmt = @Stmt + ') '
			ELSE
				SET @Stmt = @Stmt + ' AND (a.activity_name LIKE ''' + @SearchCriteria + '%'' OR t.activity_type LIKE ''' +  @SearchCriteria + '%'')) '
				
			SET @Stmt = @Stmt + 'SELECT (SELECT 200 AS statusCode, '''' AS errorMessage, (SELECT Count(*) FROM Results) AS [data.totalRecords], '
			SET @Stmt = @Stmt + '(SELECT * FROM Results ORDER BY ' + @sortColumn + ' OFFSET ' + @PageSize + ' * ' + @PageNo + ' ROWS FETCH NEXT '
			SET @Stmt = @Stmt + @PageSize + ' ROWS ONLY FOR JSON PATH) AS [data.resultSet] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result'				
		END
	END
	ELSE
	SET @Stmt = 'SELECT (SELECT 200 AS statusCode, '''' AS errorMessage, 0 AS [data.totalRecords] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result'

	EXECUTE sp_executesql @Stmt
END
GO
------------------------------------------------------------------------------------------------------------------------------
--*******************		Get Activitiy		*******************
------------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[jsp_GetActivity](@UserId VARCHAR(50), @ID INT) AS 
BEGIN
	DECLARE @SearchCriteria VARCHAR(50) = '', @SortOption VARCHAR(50) = '', @PageNo VARCHAR(3), @PageSize VARCHAR(3)

	IF TRIM(@UserId) <> ''
	BEGIN
		;WITH Results AS (SELECT a.activity_id, a.activity_name, t.activity_type, a.activity_date, a.[description]
		FROM dbo.activities AS a WITH (NOLOCK) 
		INNER JOIN dbo.activity_types AS t WITH (NOLOCK) ON a.activity_type_Id = t.activity_type_Id
		WHERE a.crtd_user = @UserId AND a.activity_id = @ID)
		SELECT (SELECT 200 AS statusCode, '' AS errorMessage, (SELECT Count(*) FROM Results) AS [data.totalRecords], 
		(SELECT * FROM Results FOR JSON PATH) AS [data.resultSet] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result
	END
	ELSE
	SELECT (SELECT 200 AS statusCode, '' AS errorMessage, 0 AS [data.totalRecords] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result
END
GO
------------------------------------------------------------------------------------------------------------------------------
--*******************		Add Activitiy		*******************
------------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[jsp_AddActivity] (@UserId VARCHAR(50), @Json NVARCHAR(MAX)) AS 
BEGIN
	DECLARE @activity_name VARCHAR(50), @activity_date DATETIME, @description VARCHAR(3500), @activity_type_Id INT, @activity_id INT

	SELECT @Json = REPLACE(@Json, ColumnName, ColumnName) 
	FROM ( VALUES ('"activity_name"'), ('"activity_date"'), ('"description"'), ('"activity_type_Id"')) AS X(ColumnName)
	
    INSERT INTO dbo.activities (activity_name, activity_date, [description], crtd_user, crtd_datetime, activity_type_Id)  
	SELECT activity_name, activity_date, [description], @UserId, GETDATE(), activity_type_Id
	FROM OPENJSON(@Json) WITH (activity_name VARCHAR(50) '$.activity_name', activity_date DATETIME '$.activity_date', 
	[description] NVARCHAR(4000) '$.description', activity_type_Id INT '$.activity_type_Id')

	SELECT @activity_id = SCOPE_IDENTITY()

	SELECT (SELECT 200 AS statusCode, '' AS errorMessage, 1 AS [data.totalRecords], (SELECT @activity_id AS activity_id FOR JSON PATH) AS [data.resultSet] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result
END
GO
------------------------------------------------------------------------------------------------------------------------------
--*******************		Json Requests		*******************
------------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[jsp_JsonRequests] (@UserId VARCHAR(50), @ID VARCHAR(50) = '') AS
BEGIN
	DECLARE @PreFix VARCHAR(50), @SqlStmt NVARCHAR(MAX) = ''

	DECLARE db_cursor CURSOR FOR 
	SELECT Action_Type FROM dbo.Json_SPs WHERE ISJSON(Json)= 1 GROUP BY Action_Type

	OPEN db_cursor  
	FETCH NEXT FROM db_cursor INTO @PreFix  

	WHILE @@FETCH_STATUS = 0  
	BEGIN  
		IF @SqlStmt <> '' 
		SET @SqlStmt = @SqlStmt + ', '

		SET @SqlStmt = @SqlStmt + 'JSON_QUERY((SELECT ' +  STUFF((SELECT ', JSON_QUERY(( ''' + [Json] + ''')) AS [' + Action_Name + ']' FROM Json_SPs 
		WHERE Action_Type = @PreFix AND ISJSON([Json])= 1 FOR XML PATH('')), 1, 1, '')  + ' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS ' + @PreFix + '_Request'

		FETCH NEXT FROM db_cursor INTO @PreFix 
	END 

	CLOSE db_cursor  
	DEALLOCATE db_cursor

	SET @SqlStmt = 'SELECT (SELECT ' + @SqlStmt + ' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result'

	EXECUTE sp_executesql @SqlStmt
END
GO
------------------------------------------------------------------------------------------------------------------------------
--*******************		Json Schema		*******************
------------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[jsp_JsonSchemas] (@UserId VARCHAR(50), @ID VARCHAR(50) = '') AS
BEGIN
	DECLARE @PreFix VARCHAR(50), @SqlStmt NVARCHAR(MAX) = ''

	DECLARE db_cursor CURSOR FOR 
	SELECT Action_Type FROM dbo.Json_SPs WHERE ISJSON(Json_Schema)= 1 GROUP BY Action_Type

	OPEN db_cursor  
	FETCH NEXT FROM db_cursor INTO @PreFix  

	WHILE @@FETCH_STATUS = 0  
	BEGIN  
		IF @SqlStmt <> '' 
		SET @SqlStmt = @SqlStmt + ', '

		SET @SqlStmt = @SqlStmt + 'JSON_QUERY((SELECT ' +  STUFF((SELECT ', JSON_QUERY(( ''' + Json_Schema + ''')) AS [' + Action_Name + ']' FROM Json_SPs 
		WHERE Action_Type = @PreFix AND ISJSON(Json_Schema)= 1 FOR XML PATH('')), 1, 1, '')  + ' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS ' + @PreFix + '_Schema'

		FETCH NEXT FROM db_cursor INTO @PreFix 
	END 

	CLOSE db_cursor  
	DEALLOCATE db_cursor

	SET @SqlStmt = 'SELECT (SELECT ' + REPLACE(LOWER(@SqlStmt), '"additionalproperties"', '"additionalProperties"') + ' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result'

	EXECUTE sp_executesql @SqlStmt
END
GO
------------------------------------------------------------------------------------------------------------------------------
--*******************		Actions		*******************
------------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[jsp_WebApiActions] (@UserId VARCHAR(50)) AS
BEGIN
	DECLARE @PreFix VARCHAR(50), @SqlStmt NVARCHAR(MAX) = ''

	DECLARE db_cursor CURSOR FOR 
	SELECT Action_Type FROM dbo.Json_SPs GROUP BY Action_Type

	OPEN db_cursor  
	FETCH NEXT FROM db_cursor INTO @PreFix  

	WHILE @@FETCH_STATUS = 0  
	BEGIN  
		IF @SqlStmt <> '' 
		SET @SqlStmt = @SqlStmt + ', '

		SET @SqlStmt = @SqlStmt + 'JSON_QUERY((SELECT ' +  STUFF((SELECT ', ''' + SP_Name + ''' AS [' + Action_Name + ']' FROM Json_SPs WHERE Action_Type = @PreFix FOR XML PATH('')), 1, 1, '')  + ' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS ' + @PreFix

		FETCH NEXT FROM db_cursor INTO @PreFix 
	END 

	CLOSE db_cursor  
	DEALLOCATE db_cursor

	SET @SqlStmt = 'SELECT (SELECT ' + @SqlStmt + ' FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS Result'

	EXECUTE sp_executesql @SqlStmt
END
GO
------------------------------------------------------------------------------------------------------------------------------
