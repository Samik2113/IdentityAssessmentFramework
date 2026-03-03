namespace IamMaturityStudio.Domain.Entities;

public class AssessmentParticipant
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}