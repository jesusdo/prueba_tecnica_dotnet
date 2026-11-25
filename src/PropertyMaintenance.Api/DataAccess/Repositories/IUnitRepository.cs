namespace PropertyMaintenance.Api.DataAccess.Repositories;
public interface IUnitRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
