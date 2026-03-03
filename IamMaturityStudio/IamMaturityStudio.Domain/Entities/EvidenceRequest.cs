namespace IamMaturityStudio.Domain.Entities;

public class EvidenceRequest
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string RequestText { get; set; } = string.Empty;
}