namespace PropertyMaintenance.Api.DataAccess.Repositories;

public interface ITechnicianRepository
{
    // Null distinguishes a missing technician from an inactive one.
    Task<bool?> GetActiveStatusAsync(int id, CancellationToken cancellationToken = default);
}
