namespace IamMaturityStudio.Infrastructure.Reports;

public sealed class ReportOptions
{
    public string StorageMode { get; set; } = "Local";
    public string LocalFolder { get; set; } = "App_Data/reports";
    public string? BlobConnectionString { get; set; }
    public string BlobContainer { get; set; } = "reports";
}
