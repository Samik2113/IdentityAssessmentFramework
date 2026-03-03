namespace IamMaturityStudio.Domain.Entities;

public class AssessmentInvitation
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTimeOffset InvitedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}