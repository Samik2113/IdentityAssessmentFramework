namespace IamMaturityStudio.Domain.Entities;

public class Domain
{
    public Guid Id { get; set; }
    public Guid QuestionnaireId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}