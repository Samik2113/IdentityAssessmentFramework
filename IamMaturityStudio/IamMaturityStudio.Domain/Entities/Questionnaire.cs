namespace IamMaturityStudio.Domain.Entities;

public class Questionnaire
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "v1";
}