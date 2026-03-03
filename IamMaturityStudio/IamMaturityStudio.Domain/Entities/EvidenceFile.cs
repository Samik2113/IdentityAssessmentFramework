namespace IamMaturityStudio.Domain.Entities;

public class EvidenceFile
{
    public Guid Id { get; set; }
    public Guid EvidenceRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
}