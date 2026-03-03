namespace IamMaturityStudio.Domain.Entities;

public class EvidenceFile
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid EvidenceRequestId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public string VirusScanStatus { get; set; } = "PendingScan";
    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}