namespace PropertyMaintenance.Api.DataAccess.Repositories;
public interface ITenantRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<PropertyMaintenance.Api.Business.Models.TenantHistoryReadModel?> GetHistoryAsync(int id, DateOnly today, CancellationToken cancellationToken = default);
}
