using FluentAssertions;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Application.Reports;
using IamMaturityStudio.Application.Services;
using IamMaturityStudio.Domain.Entities;
using IamMaturityStudio.Domain.Enums;
using IamMaturityStudio.Infrastructure.Persistence;
using IamMaturityStudio.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IamMaturityStudio.Tests.Reports;

public class ReportServiceTests
{
    [Fact]
    public async Task GenerateAsync_Returns_Stored_Url_And_Persists_Report()
    {
        await using var db = CreateDbContext();
        var context = new ApplicationDataContext(db);
        SeedMinimalAssessmentGraph(context, out var orgId, out var assessmentId);

        var service = new ReportService(
            context,
            new DashboardService(context, new ScoringService(context)),
            new FakeChartRenderer(),
            new FakePdfBuilder(),
            new FakeReportStorage(),
            NullLogger<ReportService>.Instance);

        var result = await service.GenerateAsync(
            assessmentId,
            orgId,
            new GenerateReportRequest(assessmentId, "Standard", null),
            CancellationToken.None);

        result.ReportUrl.Should().Be("https://local.test/report.pdf");
        result.FileName.Should().StartWith("iam-assessment-");
        db.Reports.Should().ContainSingle(r => r.AssessmentId == assessmentId && r.ReportUrl == result.ReportUrl);
    }

    [Fact]
    public void QuestPdfReportBuilder_Generates_Pdf_When_Charts_Are_Missing()
    {
        var builder = new QuestPdfReportBuilder();
        var model = new ReportDocumentModel(
            "Org A",
            "Assessment 2026",
            2026,
            DateTimeOffset.UtcNow,
            new DashboardResponse(
                new DashboardKpi(71.5m, 80.1m, 4, 2),
                new[] { new DomainScoreDto(Guid.NewGuid(), "D1", 71.5m, 3.57m) },
                new[] { new DashboardCategoryScore(Guid.NewGuid(), "C1", 68.2m) },
                new[] { new DashboardRadarSeries("D1", 71.5m) },
                new[] { new DashboardHeatmapCell("D1", "C1", 68.2m, "Med") }),
            new[] { new ReportQuestionRow("D1", "C1", "Q1", "Is control documented?", "Partial", "some note", 1) },
            new ReportChartArtifacts(null, null, new[] { "chart render failed" }),
            new[] { "chart render failed" });

        var pdf = builder.Build(model);

        pdf.Should().NotBeNull();
        pdf.Length.Should().BeGreaterThan(1000);
    }

    private sealed class FakeChartRenderer : IChartRenderer
    {
        public Task<ReportChartArtifacts> RenderAsync(DashboardResponse dashboard, string outputDirectory, string filePrefix, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ReportChartArtifacts(null, null, new[] { "Chart generation unavailable in test." }));
        }
    }

    private sealed class FakePdfBuilder : IReportPdfBuilder
    {
        public byte[] Build(ReportDocumentModel model)
        {
            return new byte[] { 0x25, 0x50, 0x44, 0x46 };
        }
    }

    private sealed class FakeReportStorage : IReportStorage
    {
        public Task<StoredReportLocation> SaveAsync(byte[] content, string fileName, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredReportLocation("https://local.test/report.pdf", "Local"));
        }
    }

    private static void SeedMinimalAssessmentGraph(IApplicationDataContext context, out Guid orgId, out Guid assessmentId)
    {
        orgId = Guid.NewGuid();
        var consultantUserId = Guid.NewGuid();
        var respondentUserId = Guid.NewGuid();

        var questionnaireId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        context.Add(new Organization { Id = orgId, Name = "Org A" });

        context.AddRange(new[]
        {
            new User { Id = consultantUserId, AadObjectId = consultantUserId.ToString(), Name = "Consultant", Email = "consultant@test.com" },
            new User { Id = respondentUserId, AadObjectId = respondentUserId.ToString(), Name = "Respondent", Email = "respondent@test.com" }
        });

        context.Add(new Questionnaire { Id = questionnaireId, Name = "Q", Version = "v1" });
        context.Add(new IamMaturityStudio.Domain.Entities.Domain { Id = domainId, QuestionnaireId = questionnaireId, Code = "D1", Name = "Domain" });
        context.Add(new Category { Id = categoryId, DomainId = domainId, Code = "C1", Name = "Category", Weight = 1, BusinessRisk = "High" });
        context.Add(new Question { Id = questionId, CategoryId = categoryId, Code = "Q1", Text = "Question", DefaultWeight = 1 });

        assessmentId = Guid.NewGuid();
        context.Add(new Assessment
        {
            Id = assessmentId,
            OrganizationId = orgId,
            QuestionnaireId = questionnaireId,
            Name = "Assessment",
            AssessmentYear = DateTime.UtcNow.Year,
            CreatedByUserId = consultantUserId,
            Status = AssessmentStatus.InCollection
        });

        context.Add(new AssessmentResponse
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            QuestionId = questionId,
            RespondentUserId = respondentUserId,
            Level = "Partial",
            Confidence = 3
        });

        context.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private static IamDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IamDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new IamDbContext(options);
    }
}
