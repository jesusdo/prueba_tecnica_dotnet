CREATE OR ALTER PROCEDURE dbo.usp_GetPropertiesSummary
    @From DATE,
    @To DATE
AS
BEGIN
    SET NOCOUNT ON;

	set @To = DATEADD(DAY, 1, @To);

	WITH BASE AS (
	SELECT 
		 [RequestId]
		  ,M.[UnitId]
		  ,[TenantId]
		  ,[TechnicianId]
		  ,[Category]
		  ,[PriorityId]
		  ,CASE WHEN StatusId IN (1,2,3) THEN 'Open'
				WHEN StatusId IN (4) THEN 'Closed'
			END AS Status
		  ,[StatusId]
		  ,[Description]
		  ,[CreatedAt]
		  ,[ClosedAt]
		  ,U.PropertyId
	FROM dbo.MaintenanceRequests M
	JOIN dbo.Units U ON M.UnitId = U.UnitId
	where (CreatedAt >= @From and CreatedAt <= @To) or (ClosedAt >= @From and ClosedAt <= @To)
	AND StatusId <> 5
	), COSTS_BY_REQUEST AS(
		select b.RequestId
			,sum(v.cost) costByRequest
		from base b
		join Visits v on v.RequestId = b.RequestId
		--where b.Status = 'Closed'-- comentar esta linea si tambien se van a contabilizar las solicitudes abiertas esta ambiguo en el requisito original
		group by b.RequestId
	)
	SELECT 
		P.PropertyId
		,P.Name as PropertyName
		--,P.City
		,SUM(CASE WHEN B.Status = 'Open' then 1 else 0 end) as OpenRequests
		,SUM(CASE WHEN B.Status = 'Closed' then 1 else 0 end) as ClosedRequests
		,AVG(CASE WHEN B.Status = 'Closed' then DATEDIFF(HOUR, CreatedAt, ClosedAt) end) as AvgResolutionHours
		,SUM(c.costByRequest) as TotalVisitCost
	FROM BASE B
	left join COSTS_BY_REQUEST c on b.RequestId = c.RequestId
	right JOIN dbo.Properties P ON B.PropertyId = P.PropertyId
	group by p.PropertyId, p.Name, p.City

END;
GO





