using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;

namespace IamMaturityStudio.Application.Services;

public class ScoringService : IScoringService
{
    private readonly IApplicationDataContext _dataContext;

    public ScoringService(IApplicationDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public ScoreSnapshotResponse ComputeAndPersist(Guid assessmentId, Guid orgId)
    {
        var assessment = _dataContext.Assessments.FirstOrDefault(a => a.Id == assessmentId && a.OrganizationId == orgId)
            ?? throw new InvalidOperationException("Assessment not found.");

        var scoringModel = _dataContext.OrgScoringModels.FirstOrDefault(m => m.OrganizationId == orgId && m.Name == "requested_default")
            ?? new Domain.Entities.OrgScoringModel
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                Name = "requested_default",
                ManualScore = 0,
                PartialScore = 1,
                FullyScore = 3,
                NAScore = null
            };

        if (_dataContext.OrgScoringModels.All(m => m.Id != scoringModel.Id))
        {
            _dataContext.Add(scoringModel);
        }

        var domainIds = _dataContext.Domains.Where(d => d.QuestionnaireId == assessment.QuestionnaireId).Select(d => d.Id).ToList();
        var categories = _dataContext.Categories.Where(c => domainIds.Contains(c.DomainId)).ToList();
        var categoryIds = categories.Select(c => c.Id).ToList();
        var questions = _dataContext.Questions.Where(q => categoryIds.Contains(q.CategoryId)).ToList();
        var responses = _dataContext.AssessmentResponses.Where(r => r.AssessmentId == assessmentId).ToList();

        var domainScores = new List<DomainScoreDto>();

        foreach (var domain in _dataContext.Domains.Where(d => d.QuestionnaireId == assessment.QuestionnaireId).ToList())
        {
            var domainCategories = categories.Where(c => c.DomainId == domain.Id).ToList();
            if (domainCategories.Count == 0)
            {
                domainScores.Add(new DomainScoreDto(domain.Id, domain.Code, 0, 0));
                continue;
            }

            decimal totalWeightedPercent = 0;
            decimal totalCategoryWeight = 0;

            foreach (var category in domainCategories)
            {
                var categoryQuestions = questions.Where(q => q.CategoryId == category.Id).ToList();
                decimal weightedQuestionScoreSum = 0;
                decimal questionWeightSum = 0;

                foreach (var question in categoryQuestions)
                {
                    var response = responses.FirstOrDefault(r => r.QuestionId == question.Id);
                    var levelValue = MapLevel(response?.Level, scoringModel);
                    if (levelValue is null)
                    {
                        continue;
                    }

                    weightedQuestionScoreSum += levelValue.Value * question.DefaultWeight;
                    questionWeightSum += question.DefaultWeight;
                }

                var categoryPercent = questionWeightSum == 0 ? 0 : (weightedQuestionScoreSum / questionWeightSum) / 3m * 100m;
                totalWeightedPercent += categoryPercent * category.Weight;
                totalCategoryWeight += category.Weight;
            }

            var domainPercent = totalCategoryWeight == 0 ? 0 : totalWeightedPercent / totalCategoryWeight;
            domainScores.Add(new DomainScoreDto(domain.Id, domain.Code, Math.Round(domainPercent, 2), PercentToMaturity(domainPercent)));
        }

        var overallPercent = domainScores.Count == 0 ? 0 : domainScores.Average(d => d.Percent);
        var snapshot = new ScoreSnapshotResponse(Math.Round(overallPercent, 2), PercentToMaturity(overallPercent), domainScores);

        var existingScores = _dataContext.AssessmentScores.Where(s => s.AssessmentId == assessmentId).ToList();
        foreach (var item in existingScores)
        {
            item.Percent = 0;
        }

        _dataContext.Add(new Domain.Entities.AssessmentScore
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            OrganizationId = orgId,
            ScopeType = "Overall",
            ScopeId = null,
            Percent = snapshot.OverallPercent,
            Maturity0To5 = snapshot.OverallMaturity0To5,
            ScoringModelName = scoringModel.Name
        });

        foreach (var domainScore in snapshot.Domains)
        {
            _dataContext.Add(new Domain.Entities.AssessmentScore
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                OrganizationId = orgId,
                ScopeType = "Domain",
                ScopeId = domainScore.DomainId,
                Percent = domainScore.Percent,
                Maturity0To5 = domainScore.Maturity0To5,
                ScoringModelName = scoringModel.Name
            });
        }

        _dataContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return snapshot;
    }

    private static decimal? MapLevel(string? level, Domain.Entities.OrgScoringModel model)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return null;
        }

        return level.Trim().ToLowerInvariant() switch
        {
            "manual" => model.ManualScore,
            "partial" => model.PartialScore,
            "fully" => model.FullyScore,
            "na" => model.NAScore,
            _ => null
        };
    }

    private static decimal PercentToMaturity(decimal percent)
    {
        var normalized = Math.Clamp(percent, 0, 100) / 20m;
        return Math.Round(normalized, 2);
    }
}