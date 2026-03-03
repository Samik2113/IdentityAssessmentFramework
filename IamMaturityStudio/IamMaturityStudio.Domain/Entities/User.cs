namespace IamMaturityStudio.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string AadObjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}