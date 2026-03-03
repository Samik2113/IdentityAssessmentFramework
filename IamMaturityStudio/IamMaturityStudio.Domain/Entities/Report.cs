namespace IamMaturityStudio.Domain.Entities;

public class Report
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string ReportType { get; set; } = "Standard";
    public string ReportUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}