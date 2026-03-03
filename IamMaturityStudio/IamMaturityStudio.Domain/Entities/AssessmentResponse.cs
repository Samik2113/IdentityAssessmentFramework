namespace IamMaturityStudio.Domain.Entities;

public class AssessmentResponse
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid RespondentUserId { get; set; }
    public string Level { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int? Confidence { get; set; }
    public decimal? Score { get; set; }
}