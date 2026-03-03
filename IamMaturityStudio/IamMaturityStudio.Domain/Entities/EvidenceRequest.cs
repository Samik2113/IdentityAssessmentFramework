namespace IamMaturityStudio.Domain.Entities;

public class EvidenceRequest
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid QuestionId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Open";
    public Guid CreatedByUserId { get; set; }
}