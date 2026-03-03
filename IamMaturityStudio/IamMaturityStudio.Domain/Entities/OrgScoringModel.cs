namespace IamMaturityStudio.Domain.Entities;

public class OrgScoringModel
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ManualScore { get; set; }
    public int? PartialScore { get; set; }
    public int? FullyScore { get; set; }
    public int? NAScore { get; set; }
}