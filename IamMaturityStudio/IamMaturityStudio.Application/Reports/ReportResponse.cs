namespace IamMaturityStudio.Application.Reports;

public sealed record ReportResponse(string ReportUrl, string FileName, DateTimeOffset GeneratedAtUtc);
