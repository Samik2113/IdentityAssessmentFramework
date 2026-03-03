namespace IamMaturityStudio.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CurrentScoringModelId { get; set; }
    public string? LogoUrl { get; set; }
    public string? ThemeJson { get; set; }
    public string? HeatmapBandsJson { get; set; }
}