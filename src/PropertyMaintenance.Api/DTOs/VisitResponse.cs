using PropertyMaintenance.Api.Business.Models;
namespace PropertyMaintenance.Api.DTOs;
public sealed record VisitResponse(int VisitId, int RequestId, int TechnicianId, DateTime VisitedAt, string Notes, decimal Cost)
{
    public static VisitResponse FromModel(VisitModel model) => new(model.VisitId, model.RequestId,
        model.TechnicianId, model.VisitedAt, model.Notes, model.Cost);
}
