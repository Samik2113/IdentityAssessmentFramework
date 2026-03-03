namespace IamMaturityStudio.Domain.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public decimal DefaultWeight { get; set; }
    public bool EvidenceRequired { get; set; }
    public string HelpText { get; set; } = string.Empty;
}