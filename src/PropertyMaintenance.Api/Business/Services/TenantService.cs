using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;
namespace PropertyMaintenance.Api.Business.Services;
public sealed class TenantService(ITenantRepository tenants, TimeProvider time)
{
    public async Task<TenantHistoryReadModel> GetHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new RequestException(400, "INVALID_INPUT", "TenantId must be positive.");
        return await tenants.GetHistoryAsync(id, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime), cancellationToken)
            ?? throw new RequestException(404, "TENANT_NOT_FOUND", "The tenant does not exist.");
    }
}
