namespace PropertyMaintenance.Api.DataAccess.Repositories;
public interface ITenantRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
