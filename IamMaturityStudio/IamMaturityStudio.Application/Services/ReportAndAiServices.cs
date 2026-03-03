using System.Text.RegularExpressions;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;

namespace IamMaturityStudio.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDataContext _dataContext;

    public ReportService(IApplicationDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public ReportResponse Generate(Guid assessmentId, Guid orgId, GenerateReportRequest request)
    {
        var reportUrl = $"https://reports.local/{orgId}/{assessmentId}/{Guid.NewGuid():N}.pdf";
        _dataContext.Add(new Domain.Entities.Report
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            ReportType = request.ReportType,
            ReportUrl = reportUrl
        });
        _dataContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return new ReportResponse(reportUrl);
    }
}

public class AiGuidanceService : IAiGuidanceService
{
    public AiGuidanceResponse Generate(AiGuidanceRequest request)
    {
        var redactedComment = Redact(request.UserComment ?? string.Empty);
        var explanation = $"Use least-privilege improvements for {Redact(request.Domain)} / {Redact(request.Category)} with measurable controls.";
        var examples = new[]
        {
            "Implement quarterly access recertification.",
            "Add maker-checker approval for privileged changes."
        };

        var evidenceSuggestions = new[]
        {
            "Policy document with approval workflow.",
            "Audit report for recent access reviews.",
            "System screenshot showing MFA enforcement."
        };

        var checklist = new[]
        {
            "Define control owner.",
            "Set due date and KPI.",
            string.IsNullOrWhiteSpace(redactedComment) ? "Capture contextual notes." : "Address contextual note in remediation plan."
        };

        return new AiGuidanceResponse(explanation, examples, evidenceSuggestions, checklist);
    }

    private static string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var redactedEmail = Regex.Replace(value, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", "[REDACTED_EMAIL]", RegexOptions.IgnoreCase);
        return Regex.Replace(redactedEmail, @"\b(?:org|organization|company)\s*:\s*[^,;\n]+", "organization:[REDACTED]", RegexOptions.IgnoreCase);
    }
}