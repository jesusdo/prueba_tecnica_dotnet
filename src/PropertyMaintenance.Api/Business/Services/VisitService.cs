using System.Transactions;
using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;
namespace PropertyMaintenance.Api.Business.Services;
public sealed class VisitService(IMaintenanceRequestRepository requests, ITechnicianRepository technicians,
    IVisitRepository visits, TimeProvider time)
{
    public async Task<VisitModel> CreateAsync(int requestId, int technicianId, string notes, decimal cost,
        CancellationToken cancellationToken = default)
    {
        notes = notes?.Trim() ?? "";
        if (requestId <= 0 || technicianId <= 0 || notes.Length is 0 or > 2000 || !VisitModel.IsValidCost(cost))
            throw new RequestException(400, "INVALID_INPUT", "The visit fields are invalid.");
        using var transaction = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) },
            TransactionScopeAsyncFlowOption.Enabled);
        await requests.LockForUpdateAsync(requestId, cancellationToken);
        var request = await requests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new RequestException(404, "REQUEST_NOT_FOUND", "The maintenance request does not exist.");
        if ((RequestStatusId)request.StatusId is not (RequestStatusId.Assigned or RequestStatusId.InProgress))
            throw new RequestException(409, "INVALID_VISIT_STATUS", "Visits require an Assigned or InProgress request.");
        var active = await technicians.GetActiveStatusAsync(technicianId, cancellationToken);
        if (active is null) throw new RequestException(404, "TECHNICIAN_NOT_FOUND", "The technician does not exist.");
        if (!active.Value) throw new RequestException(409, "TECHNICIAN_INACTIVE", "The technician is inactive.");
        var instant = time.GetUtcNow().UtcDateTime;
        var visitedAt = new DateTime(instant.Ticks - instant.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        if (visitedAt < request.CreatedAt)
            throw new RequestException(409, "INVALID_INPUT", "The visit time cannot precede request creation.");
        var saved = await visits.AddAsync(new VisitModel { RequestId = requestId, TechnicianId = technicianId,
            Notes = notes, Cost = cost, VisitedAt = visitedAt }, cancellationToken);
        transaction.Complete();
        return saved;
    }
}
