using PropertyMaintenance.Api.Business.Models;
using PropertyMaintenance.Api.DataAccess.Entities;
namespace PropertyMaintenance.Api.Mappings;
public static class VisitMapping
{
    public static VisitModel ToModel(this Visit entity) => new()
    {
        VisitId = entity.VisitId, RequestId = entity.RequestId, TechnicianId = entity.TechnicianId,
        VisitedAt = DateTime.SpecifyKind(entity.VisitedAt, DateTimeKind.Utc), Notes = entity.Notes ?? "", Cost = entity.Cost
    };
}
