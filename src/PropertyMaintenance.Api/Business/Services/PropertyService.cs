using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Repositories;
using PropertyMaintenance.Api.Exceptions;
namespace PropertyMaintenance.Api.Business.Services;
public sealed class PropertyService(IPropertyRepository properties, TimeProvider time)
{
    public async Task<IReadOnlyList<UnitReadModel>> GetUnitsAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new RequestException(400, "INVALID_INPUT", "PropertyId must be positive.");
        if (!await properties.ExistsAsync(id, cancellationToken))
            throw new RequestException(404, "PROPERTY_NOT_FOUND", "The property does not exist.");
        return await properties.GetUnitsAsync(id, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime), cancellationToken);
    }
}
