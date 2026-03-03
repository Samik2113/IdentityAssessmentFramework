namespace IamMaturityStudio.Domain.Entities;

public class AssessmentScore
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ScopeType { get; set; } = "Overall";
    public Guid? ScopeId { get; set; }
    public decimal Percent { get; set; }
    public decimal Maturity0To5 { get; set; }
    public string ScoringModelName { get; set; } = "requested_default";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}