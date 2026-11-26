using PropertyMaintenance.Api.Business.Models;
namespace PropertyMaintenance.Api.DataAccess.Repositories;
public interface IPropertyRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitReadModel>> GetUnitsAsync(int id, DateOnly today, CancellationToken cancellationToken = default);
}
