namespace IamMaturityStudio.Application.Reports;

public sealed record GenerateReportRequest(Guid AssessmentId, string ReportType = "Standard", string? ThemeOverride = null);
