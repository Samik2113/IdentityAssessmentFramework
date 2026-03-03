using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;

namespace IamMaturityStudio.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDataContext _dataContext;
    private readonly IScoringService _scoringService;

    public DashboardService(IApplicationDataContext dataContext, IScoringService scoringService)
    {
        _dataContext = dataContext;
        _scoringService = scoringService;
    }

    public DashboardResponse Build(Guid assessmentId, Guid orgId)
    {
        var score = _scoringService.ComputeAndPersist(assessmentId, orgId);
        var assessment = _dataContext.Assessments.First(a => a.Id == assessmentId);
        var domains = _dataContext.Domains.Where(d => d.QuestionnaireId == assessment.QuestionnaireId).ToList();
        var categories = _dataContext.Categories.Where(c => domains.Select(d => d.Id).Contains(c.DomainId)).ToList();

        var requestedEvidence = _dataContext.EvidenceRequests.Count(r => r.AssessmentId == assessmentId);
        var uploadedEvidence = _dataContext.EvidenceFiles.Count(f => f.AssessmentId == assessmentId);
        var evidenceCompletenessPercent = requestedEvidence == 0 ? 0 : Math.Round(uploadedEvidence * 100m / requestedEvidence, 2);

        var categoryScores = categories
            .Select(c =>
            {
                var domain = domains.First(d => d.Id == c.DomainId);
                var domainScore = score.Domains.FirstOrDefault(d => d.DomainId == domain.Id)?.Percent ?? 0;
                return new DashboardCategoryScore(c.Id, c.Code, domainScore);
            })
            .ToList();

        var heatmapBandsJson = _dataContext.Organizations.First(o => o.Id == orgId).HeatmapBandsJson;
        var heatmap = categoryScores
            .Select(c =>
            {
                var domainCode = domains.First(d => categories.First(cat => cat.Id == c.CategoryId).DomainId == d.Id).Code;
                var band = ResolveBand(c.Percent, heatmapBandsJson);
                return new DashboardHeatmapCell(domainCode, c.CategoryCode, c.Percent, band);
            })
            .ToList();

        var radar = score.Domains.Select(d => new DashboardRadarSeries(d.DomainCode, d.Percent)).ToList();
        var gaps = categoryScores.Count(c => c.Percent < 40);
        var quickWins = categories.Count(c => c.Weight >= 3 && categoryScores.First(s => s.CategoryId == c.Id).Percent < 55);

        return new DashboardResponse(
            new DashboardKpi(score.OverallPercent, evidenceCompletenessPercent, gaps, quickWins),
            score.Domains,
            categoryScores,
            radar,
            heatmap);
    }

    private static string ResolveBand(decimal percent, string? heatmapBandsOverride)
    {
        if (!string.IsNullOrWhiteSpace(heatmapBandsOverride))
        {
            return percent switch
            {
                < 40 => "Red",
                <= 70 => "Amber",
                _ => "Green"
            };
        }

        return percent switch
        {
            < 40 => "Red",
            <= 70 => "Amber",
            _ => "Green"
        };
    }
}