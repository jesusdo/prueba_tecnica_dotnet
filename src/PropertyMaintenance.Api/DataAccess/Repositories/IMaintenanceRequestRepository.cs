using PropertyMaintenance.Api.Business.Models;

namespace PropertyMaintenance.Api.DataAccess.Repositories;

public interface IMaintenanceRequestRepository
{
    Task<MaintenanceRequestModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MaintenanceRequestModel> AddAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default);
    Task<bool> HasVisitsAsync(int id, CancellationToken cancellationToken = default);
    Task<MaintenanceRequestModel> UpdateStatusAsync(MaintenanceRequestModel model, CancellationToken cancellationToken = default);
}
