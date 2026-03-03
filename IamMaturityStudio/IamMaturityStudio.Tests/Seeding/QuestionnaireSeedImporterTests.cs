using System.Text;
using FluentAssertions;
using IamMaturityStudio.Infrastructure.Persistence;
using IamMaturityStudio.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace IamMaturityStudio.Tests.Seeding;

public class QuestionnaireSeedImporterTests
{
    [Fact]
    public async Task ImportAsync_FirstRun_InsertsQuestionnaireAndStructure()
    {
        await using var dbContext = CreateDbContext();
        var importer = new QuestionnaireSeedImporter(dbContext);
        await using var stream = CreateSeedStream(
            questionnaireName: "IAM Questionnaire",
            categoryName: "Access Governance",
            categoryWeight: 2.5m,
            questionText: "Do you run access reviews?");

        var result = await importer.ImportAsync(stream, CancellationToken.None);

        result.DomainsInserted.Should().Be(1);
        result.CategoriesInserted.Should().Be(1);
        result.QuestionsInserted.Should().Be(1);

        dbContext.Questionnaires.Count().Should().Be(1);
        dbContext.Domains.Count().Should().Be(1);
        dbContext.Categories.Count().Should().Be(1);
        dbContext.Questions.Count().Should().Be(1);

        dbContext.Organizations.Count().Should().Be(1);
        dbContext.OrgScoringModels.Count().Should().Be(2);
        dbContext.OrgScoringModels.Should().Contain(s => s.Name == "requested_default" && s.ManualScore == 0 && s.PartialScore == 1 && s.FullyScore == 3 && s.NAScore == null);
        dbContext.OrgScoringModels.Should().Contain(s => s.Name == "recommended_default" && s.ManualScore == 1 && s.PartialScore == 3 && s.FullyScore == 5 && s.NAScore == null);
    }

    [Fact]
    public async Task ImportAsync_SecondRun_IsIdempotentAndUpdatesChangedValues()
    {
        var dbName = Guid.NewGuid().ToString("N");

        await using (var firstContext = CreateDbContext(dbName))
        {
            var importer = new QuestionnaireSeedImporter(firstContext);
            await using var firstStream = CreateSeedStream(
                questionnaireName: "IAM Questionnaire",
                categoryName: "Access Governance",
                categoryWeight: 2.5m,
                questionText: "Do you run access reviews?");

            await importer.ImportAsync(firstStream, CancellationToken.None);
        }

        await using (var secondContext = CreateDbContext(dbName))
        {
            var importer = new QuestionnaireSeedImporter(secondContext);
            await using var secondStream = CreateSeedStream(
                questionnaireName: "IAM Questionnaire",
                categoryName: "Access Governance Updated",
                categoryWeight: 3.0m,
                questionText: "Do you perform periodic access reviews?");

            var result = await importer.ImportAsync(secondStream, CancellationToken.None);

            result.DomainsInserted.Should().Be(0);
            result.CategoriesInserted.Should().Be(0);
            result.QuestionsInserted.Should().Be(0);
            result.CategoriesUpdated.Should().Be(1);
            result.QuestionsUpdated.Should().Be(1);

            secondContext.Questionnaires.Count().Should().Be(1);
            secondContext.Domains.Count().Should().Be(1);
            secondContext.Categories.Count().Should().Be(1);
            secondContext.Questions.Count().Should().Be(1);

            secondContext.Categories.Single().Name.Should().Be("Access Governance Updated");
            secondContext.Categories.Single().Weight.Should().Be(3.0m);
            secondContext.Questions.Single().Text.Should().Be("Do you perform periodic access reviews?");

            secondContext.OrgScoringModels.Count().Should().Be(2);
            secondContext.OrgScoringModels.Should().Contain(s => s.Name == "requested_default");
            secondContext.OrgScoringModels.Should().Contain(s => s.Name == "recommended_default");
        }
    }

    private static IamDbContext CreateDbContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new IamDbContext(options);
    }

    private static MemoryStream CreateSeedStream(
        string questionnaireName,
        string categoryName,
        decimal categoryWeight,
        string questionText)
    {
        var json = $$"""
                     {
                       "questionnaire": {
                         "name": "{{questionnaireName}}",
                         "domains": [
                           {
                             "code": "dom1",
                             "name": "Identity Governance",
                             "categories": [
                               {
                                 "code": "cat1",
                                 "name": "{{categoryName}}",
                                 "weight": {{categoryWeight}},
                                 "business_risk": "High",
                                 "questions": [
                                   {
                                     "code": "q1",
                                     "text": "{{questionText}}",
                                     "default_weight": 1.0,
                                     "evidence_required": true,
                                     "help_text": "Attach evidence if available"
                                   }
                                 ]
                               }
                             ]
                           }
                         ]
                       },
                       "scoring": {
                         "models": []
                       },
                       "metadata": {
                         "source": "test"
                       }
                     }
                     """;

        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}