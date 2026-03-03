namespace IamMaturityStudio.Domain.Entities;

public class AssessmentScore
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public decimal Score { get; set; }
}