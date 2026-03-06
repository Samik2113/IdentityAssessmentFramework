using System.Text.Json;
using IamMaturityStudio.Domain.Entities;
using IamMaturityStudio.Infrastructure.Persistence;
using IamMaturityStudio.Infrastructure.Seeding.Contracts;
using Microsoft.EntityFrameworkCore;
using IamDomainEntity = IamMaturityStudio.Domain.Entities.Domain;

namespace IamMaturityStudio.Infrastructure.Seeding;

public class QuestionnaireSeedImporter : IQuestionnaireSeedImporter
{
    private const string QuestionnaireVersion = "v1";

    private readonly IamDbContext _dbContext;

    public QuestionnaireSeedImporter(IamDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SeedResult> ImportAsync(Stream jsonStream, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);

        var seed = await JsonSerializer.DeserializeAsync<QuestionnaireSeedRoot>(
            jsonStream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        if (seed?.Questionnaire is null)
        {
            throw new InvalidOperationException("Invalid seed payload: questionnaire section is missing.");
        }

        var questionnaireName = seed.Questionnaire.Name?.Trim();
        if (string.IsNullOrWhiteSpace(questionnaireName))
        {
            throw new InvalidOperationException("Invalid seed payload: questionnaire.name is required.");
        }

        var domainsInserted = 0;
        var domainsUpdated = 0;
        var categoriesInserted = 0;
        var categoriesUpdated = 0;
        var questionsInserted = 0;
        var questionsUpdated = 0;

        var questionnaire = await _dbContext.Questionnaires
            .FirstOrDefaultAsync(q => q.Name == questionnaireName && q.Version == QuestionnaireVersion, ct);

        if (questionnaire is null)
        {
            questionnaire = new Questionnaire
            {
                Id = Guid.NewGuid(),
                Name = questionnaireName,
                Version = QuestionnaireVersion
            };
            _dbContext.Questionnaires.Add(questionnaire);
        }
        else
        {
            if (questionnaire.Name != questionnaireName)
            {
                questionnaire.Name = questionnaireName;
            }

            if (questionnaire.Version != QuestionnaireVersion)
            {
                questionnaire.Version = QuestionnaireVersion;
            }
        }

        foreach (var seedDomain in seed.Questionnaire.Domains)
        {
            var domainCode = Normalize(seedDomain.Code);
            if (string.IsNullOrWhiteSpace(domainCode))
            {
                continue;
            }

            var domain = _dbContext.Domains.Local
                .FirstOrDefault(d => d.QuestionnaireId == questionnaire.Id && d.Code == domainCode)
                ?? await _dbContext.Domains
                    .FirstOrDefaultAsync(d => d.QuestionnaireId == questionnaire.Id && d.Code == domainCode, ct);

            if (domain is null)
            {
                domain = new IamDomainEntity
                {
                    Id = Guid.NewGuid(),
                    QuestionnaireId = questionnaire.Id,
                    Code = domainCode,
                    Name = seedDomain.Name.Trim()
                };
                _dbContext.Domains.Add(domain);
                domainsInserted++;
            }
            else if (domain.Name != seedDomain.Name.Trim())
            {
                domain.Name = seedDomain.Name.Trim();
                domainsUpdated++;
            }

            foreach (var seedCategory in seedDomain.Categories)
            {
                var categoryCode = Normalize(seedCategory.Code);
                if (string.IsNullOrWhiteSpace(categoryCode))
                {
                    continue;
                }

                var category = _dbContext.Categories.Local
                    .FirstOrDefault(c => c.DomainId == domain.Id && c.Code == categoryCode)
                    ?? await _dbContext.Categories
                        .FirstOrDefaultAsync(c => c.DomainId == domain.Id && c.Code == categoryCode, ct);

                if (category is null)
                {
                    category = new Category
                    {
                        Id = Guid.NewGuid(),
                        DomainId = domain.Id,
                        Code = categoryCode,
                        Name = seedCategory.Name.Trim(),
                        Weight = seedCategory.Weight,
                        BusinessRisk = seedCategory.BusinessRisk.Trim()
                    };
                    _dbContext.Categories.Add(category);
                    categoriesInserted++;
                }
                else
                {
                    var changed = false;
                    if (category.Name != seedCategory.Name.Trim())
                    {
                        category.Name = seedCategory.Name.Trim();
                        changed = true;
                    }

                    if (category.Weight != seedCategory.Weight)
                    {
                        category.Weight = seedCategory.Weight;
                        changed = true;
                    }

                    if (category.BusinessRisk != seedCategory.BusinessRisk.Trim())
                    {
                        category.BusinessRisk = seedCategory.BusinessRisk.Trim();
                        changed = true;
                    }

                    if (changed)
                    {
                        categoriesUpdated++;
                    }
                }

                foreach (var seedQuestion in seedCategory.Questions)
                {
                    var questionCode = Normalize(seedQuestion.Code);
                    if (string.IsNullOrWhiteSpace(questionCode))
                    {
                        continue;
                    }

                    var question = _dbContext.Questions.Local
                        .FirstOrDefault(q => q.CategoryId == category.Id && q.Code == questionCode)
                        ?? await _dbContext.Questions
                            .FirstOrDefaultAsync(q => q.CategoryId == category.Id && q.Code == questionCode, ct);

                    if (question is null)
                    {
                        question = new Question
                        {
                            Id = Guid.NewGuid(),
                            CategoryId = category.Id,
                            Code = questionCode,
                            Text = seedQuestion.Text.Trim(),
                            DefaultWeight = seedQuestion.DefaultWeight,
                            EvidenceRequired = seedQuestion.EvidenceRequired,
                            HelpText = seedQuestion.HelpText.Trim()
                        };
                        _dbContext.Questions.Add(question);
                        questionsInserted++;
                    }
                    else
                    {
                        var changed = false;
                        if (question.Text != seedQuestion.Text.Trim())
                        {
                            question.Text = seedQuestion.Text.Trim();
                            changed = true;
                        }

                        if (question.DefaultWeight != seedQuestion.DefaultWeight)
                        {
                            question.DefaultWeight = seedQuestion.DefaultWeight;
                            changed = true;
                        }

                        if (question.EvidenceRequired != seedQuestion.EvidenceRequired)
                        {
                            question.EvidenceRequired = seedQuestion.EvidenceRequired;
                            changed = true;
                        }

                        if (question.HelpText != seedQuestion.HelpText.Trim())
                        {
                            question.HelpText = seedQuestion.HelpText.Trim();
                            changed = true;
                        }

                        if (changed)
                        {
                            questionsUpdated++;
                        }
                    }
                }
            }
        }

        var organizations = await _dbContext.Organizations.ToListAsync(ct);
        var organizationsInserted = 0;
        if (organizations.Count == 0)
        {
            var defaultOrganization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Default Org"
            };
            _dbContext.Organizations.Add(defaultOrganization);
            organizations.Add(defaultOrganization);
            organizationsInserted = 1;
        }

        var scoringModelsInserted = 0;
        foreach (var organization in organizations)
        {
            var requestedDefault = await EnsureScoringModelAsync(
                organization.Id,
                "requested_default",
                manual: 0,
                partial: 1,
                fully: 3,
                na: null,
                ct);

            var recommendedDefault = await EnsureScoringModelAsync(
                organization.Id,
                "recommended_default",
                manual: 1,
                partial: 3,
                fully: 5,
                na: null,
                ct);

            scoringModelsInserted += requestedDefault.inserted ? 1 : 0;
            scoringModelsInserted += recommendedDefault.inserted ? 1 : 0;

            organization.CurrentScoringModelId = requestedDefault.model.Id;
        }

        await _dbContext.SaveChangesAsync(ct);

        return new SeedResult(
            domainsInserted,
            domainsUpdated,
            categoriesInserted,
            categoriesUpdated,
            questionsInserted,
            questionsUpdated,
            scoringModelsInserted,
            organizationsInserted);
    }

    private async Task<(OrgScoringModel model, bool inserted)> EnsureScoringModelAsync(
        Guid organizationId,
        string modelName,
        int? manual,
        int? partial,
        int? fully,
        int? na,
        CancellationToken ct)
    {
        var model = await _dbContext.OrgScoringModels
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Name == modelName, ct);

        if (model is null)
        {
            model = new OrgScoringModel
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = modelName,
                ManualScore = manual,
                PartialScore = partial,
                FullyScore = fully,
                NAScore = na
            };
            _dbContext.OrgScoringModels.Add(model);
            return (model, true);
        }

        model.ManualScore = manual;
        model.PartialScore = partial;
        model.FullyScore = fully;
        model.NAScore = na;
        return (model, false);
    }

    private static string Normalize(string value) => value?.Trim() ?? string.Empty;
}
