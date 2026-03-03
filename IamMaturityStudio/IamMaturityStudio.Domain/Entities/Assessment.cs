using IamMaturityStudio.Domain.Enums;

namespace IamMaturityStudio.Domain.Entities;

public class Assessment
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid QuestionnaireId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AssessmentYear { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool ShowScoresToRespondents { get; set; }
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;
}