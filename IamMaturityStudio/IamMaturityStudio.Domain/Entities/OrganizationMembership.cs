namespace IamMaturityStudio.Domain.Entities;

public class OrganizationMembership
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}