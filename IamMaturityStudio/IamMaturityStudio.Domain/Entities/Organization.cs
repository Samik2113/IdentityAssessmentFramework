namespace IamMaturityStudio.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CurrentScoringModelId { get; set; }
}