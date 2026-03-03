namespace IamMaturityStudio.Application.DTOs;

public class AssessmentDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Status { get; set; } = string.Empty;
}