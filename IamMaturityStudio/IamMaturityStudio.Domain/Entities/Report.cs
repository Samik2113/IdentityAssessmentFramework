namespace IamMaturityStudio.Domain.Entities;

public class Report
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string Summary { get; set; } = string.Empty;
}