namespace PropertyMaintenance.Api.Business.Models;

public sealed class VisitModel
{
    public const decimal MaximumCost = 9999999999999999.99m;
    public static bool IsValidCost(decimal cost) => cost >= 0 && cost <= MaximumCost
        && ((decimal.GetBits(cost)[3] >> 16) & 0xff) <= 2;
    public int VisitId { get; set; }
    public int RequestId { get; set; }
    public int TechnicianId { get; set; }
    public DateTime VisitedAt { get; set; }
    public string Notes { get; set; } = "";
    public decimal Cost { get; set; }
}
