using IamMaturityStudio.Domain.Enums;

namespace IamMaturityStudio.Domain.Entities;

public class Assessment
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;
}