CREATE OR ALTER PROCEDURE dbo.usp_GetTechniciansWorkload
    @AsOfUtc DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

	WITH BASE AS (
		SELECT 
			[RequestId]
			  ,[UnitId]
			  ,[TenantId]
			  ,M.[TechnicianId]
			  ,[Category]
			  ,[PriorityId]
			  ,[StatusId]
			  ,[Description]
			  ,[CreatedAt]
			  ,T.[Name] AS TechnicianName
		  FROM [dbo].[MaintenanceRequests] M 
		  right JOIN DBO.Technicians T ON M.TechnicianId = T.TechnicianId AND T.IsActive = 1
		  WHERE StatusId IN (2,3) -- 2 ASIGNADAS, 3 EN PROGRESO
	), VISITS_B AS (
			select 
				b.RequestId
				,v.VisitId
				,v.VisitedAt
				,v.Cost
				,V.TechnicianId
			from base b
			join Visits v on v.RequestId = b.RequestId 
			AND B.TechnicianId = V.TechnicianId --amarrar las visitas por tecnico no por request
	) 
	SELECT 
		B.TechnicianId
		,B.TechnicianName
		,SUM(CASE WHEN StatusId = 2 THEN 1 ELSE 0 END) AS AssignedRequests
		,SUM(CASE WHEN StatusId = 3 THEN 1 ELSE 0 END) AS InProgressRequests
		,COUNT(distinct B.RequestId) AS TotalWorkload
		,SUM(CASE WHEN V.VisitedAt >= DATEADD(DAY, -30, GETDATE()) THEN 1 ELSE 0 END) AS VisitsLastMonth
		,SUM(V.Cost) AS TotalVisitCost 
	FROM BASE B
	LEFT join VISITS_B V ON B.RequestId = V.RequestId
	GROUP BY B.TechnicianId
		,B.TechnicianName
	ORDER BY COUNT(B.RequestId) DESC, B.TechnicianId ASC

END;
GO


