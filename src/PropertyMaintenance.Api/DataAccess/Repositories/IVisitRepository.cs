using PropertyMaintenance.Api.Business.Models;
namespace PropertyMaintenance.Api.DataAccess.Repositories;
public interface IVisitRepository
{
    Task<VisitModel> AddAsync(VisitModel model, CancellationToken cancellationToken = default);
}
