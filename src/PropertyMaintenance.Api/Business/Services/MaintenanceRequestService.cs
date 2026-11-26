using System.Transactions;
using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.Api.Business.Services;

public sealed class MaintenanceRequestService(IUnitRepository units, ITenantRepository tenants,
    ILeaseRepository leases, IMaintenanceRequestRepository requests, TimeProvider time, ITechnicianRepository technicians)
{
    public async Task<MaintenanceRequestModel> ChangeStatusAsync(int id, RequestStatusId target, int? technicianId,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0 || !Enum.IsDefined(target) || technicianId <= 0
            || (target == RequestStatusId.Assigned) != technicianId.HasValue)
            throw new RequestException(400, "INVALID_INPUT", "The status or technician input is invalid.");

        using var transaction = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromSeconds(30) },
            TransactionScopeAsyncFlowOption.Enabled);
        var model = await requests.GetByIdAsync(id, cancellationToken)
            ?? throw new RequestException(404, "REQUEST_NOT_FOUND", "The maintenance request does not exist.");
        // Keep the original RowVersion, but coordinate parent/child lock order
        // with visit registration before touching technicians or visits.
        await requests.LockForUpdateAsync(id, cancellationToken);
        if (!MaintenanceRequestModel.CanTransition((RequestStatusId)model.StatusId, target))
            throw new RequestException(409, "INVALID_STATUS_TRANSITION", "The requested status transition is not allowed.");
        if (target == RequestStatusId.Assigned)
        {
            var active = await technicians.GetActiveStatusAsync(technicianId!.Value, cancellationToken);
            if (active is null) throw new RequestException(404, "TECHNICIAN_NOT_FOUND", "The technician does not exist.");
            if (!active.Value) throw new RequestException(409, "TECHNICIAN_INACTIVE", "The technician is inactive.");
            model.TechnicianId = technicianId;
        }
        if (target == RequestStatusId.Closed && !await requests.HasVisitsAsync(id, cancellationToken))
            throw new RequestException(409, "VISIT_REQUIRED", "At least one visit is required to close the request.");
        if (target is RequestStatusId.Closed or RequestStatusId.Cancelled)
        {
            var now = time.GetUtcNow().UtcDateTime;
            var closed = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
            if (closed < model.CreatedAt)
                throw new RequestException(409, "INVALID_STATUS_TRANSITION", "The close time cannot precede creation.");
            model.ClosedAt = closed;
        }
        model.StatusId = (byte)target;
        var saved = await requests.UpdateStatusAsync(model, cancellationToken);
        transaction.Complete();
        return saved;
    }

    public async Task<MaintenanceRequestModel> CreateAsync(int unitId, int tenantId, string category,
        RequestPriorityId priority, string description, CancellationToken cancellationToken = default)
    {
        category = category?.Trim() ?? "";
        description = description?.Trim() ?? "";
        if (unitId <= 0 || tenantId <= 0 || category.Length is 0 or > 100
            || description.Length is 0 or > 2000 || !Enum.IsDefined(priority))
            throw new RequestException(400, "INVALID_INPUT", "The request fields are invalid.");

        // A single ambient local SQL transaction covers every repository using
        // the scoped context. Shared/range locks survive until insertion commits.
        using var transaction = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.Serializable, Timeout = TimeSpan.FromSeconds(30) },
            TransactionScopeAsyncFlowOption.Enabled);
        var instant = time.GetUtcNow().UtcDateTime;
        // Match datetime2(3) without rounding into the next contractual day.
        var now = new DateTime(instant.Ticks - instant.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        if (!await units.ExistsAsync(unitId, cancellationToken))
            throw new RequestException(404, "UNIT_NOT_FOUND", "The unit does not exist.");
        if (!await tenants.ExistsAsync(tenantId, cancellationToken))
            throw new RequestException(404, "TENANT_NOT_FOUND", "The tenant does not exist.");
        if (!await leases.HasActiveLeaseAsync(unitId, tenantId, DateOnly.FromDateTime(now), cancellationToken))
            throw new RequestException(409, "ACTIVE_LEASE_REQUIRED", "An active lease for this unit and tenant is required.");
        var saved = await requests.AddAsync(new MaintenanceRequestModel
        {
            UnitId = unitId, TenantId = tenantId, Category = category, Description = description,
            PriorityId = (byte)priority, StatusId = (byte)RequestStatusId.Open, CreatedAt = now
        }, cancellationToken);
        transaction.Complete();
        return saved;
    }
}
