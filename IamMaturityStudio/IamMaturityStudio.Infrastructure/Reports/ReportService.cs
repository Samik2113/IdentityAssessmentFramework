using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Application.Reports;
using IamMaturityStudio.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IamMaturityStudio.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    private readonly IApplicationDataContext _dataContext;
    private readonly IDashboardService _dashboardService;
    private readonly IChartRenderer _chartRenderer;
    private readonly IReportPdfBuilder _pdfBuilder;
    private readonly IReportStorage _reportStorage;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IApplicationDataContext dataContext,
        IDashboardService dashboardService,
        IChartRenderer chartRenderer,
        IReportPdfBuilder pdfBuilder,
        IReportStorage reportStorage,
        ILogger<ReportService> logger)
    {
        _dataContext = dataContext;
        _dashboardService = dashboardService;
        _chartRenderer = chartRenderer;
        _pdfBuilder = pdfBuilder;
        _reportStorage = reportStorage;
        _logger = logger;
    }

    public async Task<ReportResponse> GenerateAsync(Guid assessmentId, Guid orgId, GenerateReportRequest request, CancellationToken cancellationToken)
    {
        if (request.AssessmentId != Guid.Empty && request.AssessmentId != assessmentId)
        {
            throw new InvalidOperationException("Assessment id in payload does not match route.");
        }

        var assessment = _dataContext.Assessments.FirstOrDefault(a => a.Id == assessmentId && a.OrganizationId == orgId)
            ?? throw new InvalidOperationException("Assessment not found in organization scope.");

        var organization = _dataContext.Organizations.FirstOrDefault(o => o.Id == orgId)
            ?? throw new InvalidOperationException("Organization not found.");

        var dashboard = _dashboardService.Build(assessmentId, orgId);
        var questionRows = BuildQuestionRows(assessment);

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var fileName = ReportFileName.Create(organization.Name, assessment.AssessmentYear, generatedAtUtc);
        var chartOutputFolder = Path.Combine(Path.GetTempPath(), "iam-reports", assessmentId.ToString("N"));

        var chartArtifacts = await _chartRenderer.RenderAsync(dashboard, chartOutputFolder, Path.GetFileNameWithoutExtension(fileName), cancellationToken);
        var warnings = new List<string>(chartArtifacts.Warnings);

        var documentModel = new ReportDocumentModel(
            organization.Name,
            assessment.Name,
            assessment.AssessmentYear,
            generatedAtUtc,
            dashboard,
            questionRows,
            chartArtifacts,
            warnings);

        var pdfBytes = _pdfBuilder.Build(documentModel);
        var storageLocation = await _reportStorage.SaveAsync(pdfBytes, fileName, cancellationToken);

        _dataContext.Add(new Report
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            ReportType = request.ReportType,
            ReportUrl = storageLocation.ReportUrl,
            CreatedAtUtc = generatedAtUtc
        });
        await _dataContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Report generated for assessment {AssessmentId} in org {OrgId}. Type={ReportType}, Storage={StorageMode}, RadarChart={RadarChart}, HeatmapChart={HeatmapChart}",
            assessmentId,
            orgId,
            request.ReportType,
            storageLocation.StorageMode,
            chartArtifacts.RadarChartPath is not null,
            chartArtifacts.HeatmapChartPath is not null);

        return new ReportResponse(storageLocation.ReportUrl, fileName, generatedAtUtc);
    }

    private IReadOnlyList<ReportQuestionRow> BuildQuestionRows(Assessment assessment)
    {
        var domains = _dataContext.Domains.Where(d => d.QuestionnaireId == assessment.QuestionnaireId).ToList();
        var domainById = domains.ToDictionary(d => d.Id);

        var categories = _dataContext.Categories.Where(c => domainById.Keys.Contains(c.DomainId)).ToList();
        var categoryById = categories.ToDictionary(c => c.Id);

        var questions = _dataContext.Questions.Where(q => categoryById.Keys.Contains(q.CategoryId)).ToList();
        var responses = _dataContext.AssessmentResponses.Where(r => r.AssessmentId == assessment.Id).ToList();

        var evidenceFiles = _dataContext.EvidenceFiles
            .Where(e => e.AssessmentId == assessment.Id)
            .ToList();

        var evidenceCountByQuestion = evidenceFiles
            .GroupBy(e => e.QuestionId)
            .ToDictionary(g => g.Key, g => g.Count());

        var responseByQuestion = responses
            .GroupBy(r => r.QuestionId)
            .ToDictionary(g => g.Key, g => g.First());

        return questions
            .OrderBy(q => q.Code, StringComparer.OrdinalIgnoreCase)
            .Select(question =>
            {
                var category = categoryById[question.CategoryId];
                var domain = domainById[category.DomainId];
                responseByQuestion.TryGetValue(question.Id, out var response);
                evidenceCountByQuestion.TryGetValue(question.Id, out var evidenceCount);

                return new ReportQuestionRow(
                    domain.Code,
                    category.Code,
                    question.Code,
                    question.Text,
                    response?.Level ?? "NotAnswered",
                    response?.Comment,
                    evidenceCount);
            })
            .ToList();
    }
}

public static class ReportFileName
{
    public static string Create(string organizationName, int assessmentYear, DateTimeOffset generatedAtUtc)
    {
        var orgSlug = Slugify(organizationName);
        var stamp = generatedAtUtc.UtcDateTime.ToString("yyyyMMddHHmmss");
        return $"iam-assessment-{orgSlug}-{assessmentYear}-{stamp}.pdf";
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "org";
        }

        var chars = input.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "org" : slug;
    }
}
