using System.Text.Json;
using FluentAssertions;
using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Features;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Application.Reports;
using IamMaturityStudio.Application.Security;
using IamMaturityStudio.Application.Services;
using IamMaturityStudio.Domain.Entities;
using IamMaturityStudio.Domain.Enums;
using IamMaturityStudio.Infrastructure.Persistence;
using IamMaturityStudio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace IamMaturityStudio.Tests.CoreApi;

public class CoreApiSecurityAndScoringTests
{
    [Fact]
    public async Task Respondent_Cannot_Call_Score_Or_Dashboard()
    {
        await using var db = CreateDbContext();
        var context = new ApplicationDataContext(db);
        SeedMinimalAssessmentGraph(context, out var orgId, out var assessmentId, out _, out var respondentUserId);

        var handlers = CreateHandlers(context);
        var respondent = new RequesterContext(respondentUserId, "Resp", "resp@test.com", new HashSet<string> { "ClientRespondent" }, new[] { orgId });

        Func<Task> scoreCall = async () => await handlers.Handle(new ComputeScoreCommand(respondent, assessmentId), CancellationToken.None);
        Func<Task> dashboardCall = async () => await handlers.Handle(new GetDashboardQuery(respondent, assessmentId), CancellationToken.None);

        await scoreCall.Should().ThrowAsync<ForbiddenAccessException>();
        await dashboardCall.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Respondent_Response_List_Does_Not_Expose_Score_Field()
    {
        await using var db = CreateDbContext();
        var context = new ApplicationDataContext(db);
        SeedMinimalAssessmentGraph(context, out var orgId, out var assessmentId, out _, out var respondentUserId);

        var response = db.AssessmentResponses.First();
        response.Score = 99m;
        await db.SaveChangesAsync();

        var handlers = CreateHandlers(context);
        var respondent = new RequesterContext(respondentUserId, "Resp", "resp@test.com", new HashSet<string> { "ClientRespondent" }, new[] { orgId });

        var result = await handlers.Handle(new GetResponsesForRespondentQuery(respondent, assessmentId), CancellationToken.None);
        var json = JsonSerializer.Serialize(result);

        json.ToLowerInvariant().Should().NotContain("score");
    }

    [Fact]
    public void ScoringService_Excludes_NA_And_Uses_Requested_Default_Mapping()
    {
        using var db = CreateDbContext();
        var context = new ApplicationDataContext(db);

        SeedMinimalAssessmentGraph(context, out var orgId, out var assessmentId, out _, out _);

        var scorer = new ScoringService(context);
        var snapshot = scorer.ComputeAndPersist(assessmentId, orgId);

        snapshot.OverallPercent.Should().Be(33.33m);
        snapshot.Domains.Should().HaveCount(1);
        snapshot.Domains[0].Percent.Should().Be(33.33m);
    }

    [Fact]
    public async Task Cannot_Finalize_When_Open_Request_Or_PendingScan_File_Exists()
    {
        await using var db = CreateDbContext();
        var context = new ApplicationDataContext(db);
        SeedMinimalAssessmentGraph(context, out var orgId, out var assessmentId, out var consultantUserId, out _);

        var assessment = db.Assessments.First(a => a.Id == assessmentId);
        assessment.Status = AssessmentStatus.UnderReview;

        var evidenceRequest = new EvidenceRequest
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            QuestionId = db.Questions.First().Id,
            Status = "Open",
            CreatedByUserId = consultantUserId
        };
        db.EvidenceRequests.Add(evidenceRequest);

        db.EvidenceFiles.Add(new EvidenceFile
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            QuestionId = db.Questions.First().Id,
            EvidenceRequestId = evidenceRequest.Id,
            UploadedByUserId = consultantUserId,
            FileName = "evidence.pdf",
            FileType = "application/pdf",
            BlobName = "blob",
            VirusScanStatus = "PendingScan"
        });

        await db.SaveChangesAsync();

        var handlers = CreateHandlers(context);
        var consultant = new RequesterContext(consultantUserId, "Consultant", "consultant@test.com", new HashSet<string> { "Consultant" }, new[] { orgId });

        Func<Task> finalizeCall = async () => await handlers.Handle(
            new UpdateAssessmentStatusCommand(consultant, assessmentId, new UpdateAssessmentStatusRequest("Finalized")),
            CancellationToken.None);

        await finalizeCall.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Evidence_Upload_Request_Returns_Sas_And_Complete_Registers_PendingScan()
    {
        await using var db = CreateDbContext();
        var context = new ApplicationDataContext(db);
        SeedMinimalAssessmentGraph(context, out var orgId, out var assessmentId, out var consultantUserId, out var respondentUserId);

        var questionId = db.Questions.First().Id;
        var evidenceRequestId = Guid.NewGuid();
        db.EvidenceRequests.Add(new EvidenceRequest
        {
            Id = evidenceRequestId,
            AssessmentId = assessmentId,
            QuestionId = questionId,
            Status = "Open",
            CreatedByUserId = consultantUserId
        });
        await db.SaveChangesAsync();

        var handlers = CreateHandlers(context);
        var respondent = new RequesterContext(respondentUserId, "Resp", "resp@test.com", new HashSet<string> { "ClientRespondent" }, new[] { orgId });

        var sas = await handlers.Handle(
            new CreateEvidenceUploadCommand(respondent, assessmentId, new CreateEvidenceUploadRequest(evidenceRequestId, "evidence.pdf", "application/pdf", 1024)),
            CancellationToken.None);

        sas.UploadUrl.Should().NotBeNullOrWhiteSpace();
        sas.BlobName.Should().StartWith($"evidence/{orgId}/{assessmentId}/{questionId}/");

        var complete = await handlers.Handle(
            new CompleteEvidenceUploadCommand(respondent, assessmentId, new CompleteEvidenceUploadRequest(evidenceRequestId, sas.BlobName, "evidence.pdf", "application/pdf")),
            CancellationToken.None);

        complete.VirusScanStatus.Should().Be("PendingScan");
        db.EvidenceFiles.Should().Contain(f => f.Id == complete.EvidenceFileId && f.VirusScanStatus == "PendingScan");
    }

    [Fact]
    public async Task Finalize_Succeeds_When_No_Open_Requests_And_No_PendingScan()
    {
        await using var db = CreateDbContext();
        var context = new ApplicationDataContext(db);
        SeedMinimalAssessmentGraph(context, out var orgId, out var assessmentId, out var consultantUserId, out _);

        var assessment = db.Assessments.First(a => a.Id == assessmentId);
        assessment.Status = AssessmentStatus.UnderReview;

        var evidenceRequest = new EvidenceRequest
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            QuestionId = db.Questions.First().Id,
            Status = "Approved",
            CreatedByUserId = consultantUserId
        };
        db.EvidenceRequests.Add(evidenceRequest);

        db.EvidenceFiles.Add(new EvidenceFile
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            QuestionId = evidenceRequest.QuestionId,
            EvidenceRequestId = evidenceRequest.Id,
            UploadedByUserId = consultantUserId,
            FileName = "evidence.pdf",
            FileType = "application/pdf",
            BlobName = $"evidence/{orgId}/{assessmentId}/{evidenceRequest.QuestionId}/file.pdf",
            VirusScanStatus = "Clean"
        });

        await db.SaveChangesAsync();

        var handlers = CreateHandlers(context);
        var consultant = new RequesterContext(consultantUserId, "Consultant", "consultant@test.com", new HashSet<string> { "Consultant" }, new[] { orgId });

        var result = await handlers.Handle(
            new UpdateAssessmentStatusCommand(consultant, assessmentId, new UpdateAssessmentStatusRequest("Finalized")),
            CancellationToken.None);

        result.Status.Should().Be("Finalized");
    }

    private static CoreApiHandlers CreateHandlers(IApplicationDataContext context)
    {
        var scoringService = new ScoringService(context);
        var dashboardService = new DashboardService(context, scoringService);
        var reportService = new TestReportService();
        var aiService = new TestAiGuidanceService();
        var storageService = new StorageSasService();
        var evidenceScanService = new NoOpEvidenceScanService();

        return new CoreApiHandlers(
            context,
            storageService,
            evidenceScanService,
            scoringService,
            dashboardService,
            reportService,
            aiService);
    }

    private sealed class NoOpEvidenceScanService : IEvidenceScanService
    {
        public Task QueueScanAsync(Guid evidenceFileId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestAiGuidanceService : IAiGuidanceService
    {
        public Task<AiGuidanceResponse> GenerateAsync(AiGuidanceRequest request, Guid orgId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AiGuidanceResponse(
                "test",
                new[] { "example" },
                new[] { "evidence" },
                new[] { "check" }));
        }
    }

    private sealed class TestReportService : IReportService
    {
        public Task<ReportResponse> GenerateAsync(Guid assessmentId, Guid orgId, GenerateReportRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ReportResponse("https://reports.local/test.pdf", "test.pdf", DateTimeOffset.UtcNow));
        }
    }

    private static void SeedMinimalAssessmentGraph(
        IApplicationDataContext context,
        out Guid orgId,
        out Guid assessmentId,
        out Guid consultantUserId,
        out Guid respondentUserId)
    {
        orgId = Guid.NewGuid();
        consultantUserId = Guid.NewGuid();
        respondentUserId = Guid.NewGuid();

        var questionnaireId = Guid.NewGuid();
        var domainId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var questionManualId = Guid.NewGuid();
        var questionNaId = Guid.NewGuid();

        context.Add(new Organization { Id = orgId, Name = "Org A" });

        context.AddRange(new[]
        {
            new User { Id = consultantUserId, AadObjectId = consultantUserId.ToString(), Name = "Consultant", Email = "consultant@test.com" },
            new User { Id = respondentUserId, AadObjectId = respondentUserId.ToString(), Name = "Respondent", Email = "respondent@test.com" }
        });

        context.AddRange(new[]
        {
            new OrganizationMembership { Id = Guid.NewGuid(), OrganizationId = orgId, UserId = consultantUserId, Role = "Consultant" },
            new OrganizationMembership { Id = Guid.NewGuid(), OrganizationId = orgId, UserId = respondentUserId, Role = "ClientRespondent" }
        });

        context.Add(new Questionnaire { Id = questionnaireId, Name = "Q", Version = "v1" });
        context.Add(new IamMaturityStudio.Domain.Entities.Domain { Id = domainId, QuestionnaireId = questionnaireId, Code = "D1", Name = "Domain" });
        context.Add(new Category { Id = categoryId, DomainId = domainId, Code = "C1", Name = "Category", Weight = 1, BusinessRisk = "High" });

        context.AddRange(new[]
        {
            new Question { Id = questionManualId, CategoryId = categoryId, Code = "Q1", Text = "Q1", DefaultWeight = 1 },
            new Question { Id = questionNaId, CategoryId = categoryId, Code = "Q2", Text = "Q2", DefaultWeight = 1 }
        });

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

        context.Add(new AssessmentParticipant
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            UserId = respondentUserId,
            Role = "ClientRespondent"
        });

        context.AddRange(new[]
        {
            new AssessmentResponse
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                QuestionId = questionManualId,
                RespondentUserId = respondentUserId,
                Level = "Partial",
                Confidence = 3
            },
            new AssessmentResponse
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                QuestionId = questionNaId,
                RespondentUserId = respondentUserId,
                Level = "NA",
                Confidence = 3
            }
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