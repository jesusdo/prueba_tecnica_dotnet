using System.Transactions;
using PropertyMaintenance.Api.Business.Enums;
using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;

namespace PropertyMaintenance.Api.Business.Services;

public sealed class MaintenanceRequestService(IUnitRepository units, ITenantRepository tenants,
    ILeaseRepository leases, IMaintenanceRequestRepository requests, TimeProvider time)
{
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
