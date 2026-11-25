namespace PropertyMaintenance.Api.DataAccess.Repositories;

public interface ILeaseRepository
{
    Task<bool> HasActiveLeaseAsync(int unitId, int tenantId, DateOnly date, CancellationToken cancellationToken = default);
}
