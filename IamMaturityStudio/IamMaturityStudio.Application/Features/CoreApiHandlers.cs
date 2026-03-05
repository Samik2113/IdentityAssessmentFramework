using IamMaturityStudio.Application.Contracts;
using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Application.Security;
using IamMaturityStudio.Domain.Entities;
using IamMaturityStudio.Domain.Enums;
using MediatR;
using IamDomain = IamMaturityStudio.Domain.Entities.Domain;

namespace IamMaturityStudio.Application.Features;

public record GetOrganizationsQuery(RequesterContext Requester) : IRequest<IReadOnlyList<Organization>>;
public record CreateOrganizationCommand(RequesterContext Requester, CreateOrgRequest Request) : IRequest<Organization>;
public record GetOrgBrandingQuery(RequesterContext Requester, Guid OrgId) : IRequest<UpdateOrgBrandingRequest>;
public record UpdateOrgBrandingCommand(RequesterContext Requester, Guid OrgId, UpdateOrgBrandingRequest Request) : IRequest<Organization>;

public record GetQuestionnairesQuery(RequesterContext Requester) : IRequest<IReadOnlyList<Questionnaire>>;
public record GetQuestionnaireTreeQuery(RequesterContext Requester, Guid QuestionnaireId) : IRequest<QuestionnaireTreeResponse>;

public record GetAssessmentsQueryV2(RequesterContext Requester) : IRequest<IReadOnlyList<AssessmentSummaryResponse>>;
public record CreateAssessmentCommand(RequesterContext Requester, CreateAssessmentRequest Request) : IRequest<AssessmentSummaryResponse>;
public record GetAssessmentForConsultantQuery(RequesterContext Requester, Guid AssessmentId) : IRequest<AssessmentDetailForConsultantResponse>;
public record GetAssessmentForRespondentQuery(RequesterContext Requester, Guid AssessmentId) : IRequest<AssessmentDetailForRespondentResponse>;
public record UpdateAssessmentStatusCommand(RequesterContext Requester, Guid AssessmentId, UpdateAssessmentStatusRequest Request) : IRequest<AssessmentSummaryResponse>;
public record InviteParticipantsCommand(RequesterContext Requester, Guid AssessmentId, InviteParticipantsRequest Request) : IRequest<int>;

public record GetResponsesForConsultantQuery(RequesterContext Requester, Guid AssessmentId) : IRequest<IReadOnlyList<ResponseListForConsultant>>;
public record GetResponsesForRespondentQuery(RequesterContext Requester, Guid AssessmentId) : IRequest<IReadOnlyList<ResponseListForRespondent>>;
public record BulkUpsertResponsesCommand(RequesterContext Requester, Guid AssessmentId, BulkUpsertResponsesRequest Request) : IRequest<int>;
public record UpsertResponseCommand(RequesterContext Requester, Guid AssessmentId, Guid QuestionId, UpsertResponseRequest Request) : IRequest<ResponseListForRespondent>;

public record CreateEvidenceRequestCommand(RequesterContext Requester, Guid AssessmentId, CreateEvidenceRequestRequest Request) : IRequest<EvidenceRequestDto>;
public record GetEvidenceRequestsQuery(RequesterContext Requester, Guid AssessmentId) : IRequest<IReadOnlyList<EvidenceRequestDto>>;
public record UpdateEvidenceRequestStatusCommand(RequesterContext Requester, Guid AssessmentId, Guid RequestId, UpdateEvidenceRequestStatusRequest Request) : IRequest<EvidenceRequestDto>;
public record CreateEvidenceUploadCommand(RequesterContext Requester, Guid AssessmentId, CreateEvidenceUploadRequest Request) : IRequest<CreateEvidenceUploadResponse>;
public record CompleteEvidenceUploadCommand(RequesterContext Requester, Guid AssessmentId, CompleteEvidenceUploadRequest Request) : IRequest<CompleteEvidenceUploadResponse>;

public record ComputeScoreCommand(RequesterContext Requester, Guid AssessmentId) : IRequest<ScoreSnapshotResponse>;
public record GetDashboardQuery(RequesterContext Requester, Guid AssessmentId) : IRequest<DashboardResponse>;
public record GenerateReportCommand(RequesterContext Requester, Guid AssessmentId, GenerateReportRequest Request) : IRequest<ReportResponse>;
public record GetAiGuidanceCommand(RequesterContext Requester, AiGuidanceRequest Request) : IRequest<AiGuidanceResponse>;

public class CoreApiHandlers :
    IRequestHandler<GetOrganizationsQuery, IReadOnlyList<Organization>>,
    IRequestHandler<CreateOrganizationCommand, Organization>,
    IRequestHandler<GetOrgBrandingQuery, UpdateOrgBrandingRequest>,
    IRequestHandler<UpdateOrgBrandingCommand, Organization>,
    IRequestHandler<GetQuestionnairesQuery, IReadOnlyList<Questionnaire>>,
    IRequestHandler<GetQuestionnaireTreeQuery, QuestionnaireTreeResponse>,
    IRequestHandler<GetAssessmentsQueryV2, IReadOnlyList<AssessmentSummaryResponse>>,
    IRequestHandler<CreateAssessmentCommand, AssessmentSummaryResponse>,
    IRequestHandler<GetAssessmentForConsultantQuery, AssessmentDetailForConsultantResponse>,
    IRequestHandler<GetAssessmentForRespondentQuery, AssessmentDetailForRespondentResponse>,
    IRequestHandler<UpdateAssessmentStatusCommand, AssessmentSummaryResponse>,
    IRequestHandler<InviteParticipantsCommand, int>,
    IRequestHandler<GetResponsesForConsultantQuery, IReadOnlyList<ResponseListForConsultant>>,
    IRequestHandler<GetResponsesForRespondentQuery, IReadOnlyList<ResponseListForRespondent>>,
    IRequestHandler<BulkUpsertResponsesCommand, int>,
    IRequestHandler<UpsertResponseCommand, ResponseListForRespondent>,
    IRequestHandler<CreateEvidenceRequestCommand, EvidenceRequestDto>,
    IRequestHandler<GetEvidenceRequestsQuery, IReadOnlyList<EvidenceRequestDto>>,
    IRequestHandler<UpdateEvidenceRequestStatusCommand, EvidenceRequestDto>,
    IRequestHandler<CreateEvidenceUploadCommand, CreateEvidenceUploadResponse>,
    IRequestHandler<CompleteEvidenceUploadCommand, CompleteEvidenceUploadResponse>,
    IRequestHandler<ComputeScoreCommand, ScoreSnapshotResponse>,
    IRequestHandler<GetDashboardQuery, DashboardResponse>,
    IRequestHandler<GenerateReportCommand, ReportResponse>,
    IRequestHandler<GetAiGuidanceCommand, AiGuidanceResponse>
{
    private readonly IApplicationDataContext _dataContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IEvidenceScanService _evidenceScanService;
    private readonly IScoringService _scoringService;
    private readonly IDashboardService _dashboardService;
    private readonly IReportService _reportService;
    private readonly IAiGuidanceService _aiGuidanceService;

    public CoreApiHandlers(
        IApplicationDataContext dataContext,
        IBlobStorageService blobStorageService,
        IEvidenceScanService evidenceScanService,
        IScoringService scoringService,
        IDashboardService dashboardService,
        IReportService reportService,
        IAiGuidanceService aiGuidanceService)
    {
        _dataContext = dataContext;
        _blobStorageService = blobStorageService;
        _evidenceScanService = evidenceScanService;
        _scoringService = scoringService;
        _dashboardService = dashboardService;
        _reportService = reportService;
        _aiGuidanceService = aiGuidanceService;
    }

    public Task<IReadOnlyList<Organization>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        if (!request.Requester.Roles.Contains("Admin"))
        {
            throw new ForbiddenAccessException("Admin role required.");
        }

        return Task.FromResult<IReadOnlyList<Organization>>(_dataContext.Organizations.OrderBy(o => o.Name).ToList());
    }

    public async Task<Organization> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        if (!request.Requester.Roles.Contains("Admin"))
        {
            throw new ForbiddenAccessException("Admin role required.");
        }

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Request.Name.Trim(),
            ThemeJson = request.Request.ThemeJson
        };
        _dataContext.Add(org);
        await _dataContext.SaveChangesAsync(cancellationToken);
        return org;
    }

    public Task<UpdateOrgBrandingRequest> Handle(GetOrgBrandingQuery request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureOrgMember(request.Requester, request.OrgId);
        var org = _dataContext.Organizations.FirstOrDefault(o => o.Id == request.OrgId) ?? throw new NotFoundException("Organization not found.");
        return Task.FromResult(new UpdateOrgBrandingRequest(org.LogoUrl, org.ThemeJson, org.HeatmapBandsJson));
    }

    public async Task<Organization> Handle(UpdateOrgBrandingCommand request, CancellationToken cancellationToken)
    {
        if (!request.Requester.Roles.Contains("Admin"))
        {
            throw new ForbiddenAccessException("Admin role required.");
        }

        AccessGuards.EnsureOrgMember(request.Requester, request.OrgId);
        var org = _dataContext.Organizations.FirstOrDefault(o => o.Id == request.OrgId) ?? throw new NotFoundException("Organization not found.");
        org.LogoUrl = request.Request.LogoUrl;
        org.ThemeJson = request.Request.ThemeJson;
        org.HeatmapBandsJson = request.Request.HeatmapBands;
        await _dataContext.SaveChangesAsync(cancellationToken);
        return org;
    }

    public Task<IReadOnlyList<Questionnaire>> Handle(GetQuestionnairesQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Questionnaire>>(_dataContext.Questionnaires.OrderBy(q => q.Name).ToList());
    }

    public Task<QuestionnaireTreeResponse> Handle(GetQuestionnaireTreeQuery request, CancellationToken cancellationToken)
    {
        var questionnaire = _dataContext.Questionnaires.FirstOrDefault(q => q.Id == request.QuestionnaireId)
            ?? throw new NotFoundException("Questionnaire not found.");

        var domains = _dataContext.Domains.Where(d => d.QuestionnaireId == questionnaire.Id).OrderBy(d => d.Name).ToList();
        var domainIds = domains.Select(d => d.Id).ToList();
        var categories = _dataContext.Categories.Where(c => domainIds.Contains(c.DomainId)).OrderBy(c => c.Name).ToList();
        var categoryIds = categories.Select(c => c.Id).ToList();
        var questions = _dataContext.Questions.Where(q => categoryIds.Contains(q.CategoryId)).OrderBy(q => q.Code).ToList();

        var response = new QuestionnaireTreeResponse(
            questionnaire.Id,
            questionnaire.Name,
            questionnaire.Version,
            domains.Select(domain => new QuestionnaireDomainNode(
                domain.Id,
                domain.Code,
                domain.Name,
                categories.Where(c => c.DomainId == domain.Id)
                    .Select(category => new QuestionnaireCategoryNode(
                        category.Id,
                        category.Code,
                        category.Name,
                        category.Weight,
                        category.BusinessRisk,
                        questions.Where(q => q.CategoryId == category.Id)
                            .Select(question => new QuestionnaireQuestionNode(question.Id, question.Code, question.Text, question.DefaultWeight))
                            .ToList()))
                    .ToList()))
            .ToList());

        return Task.FromResult(response);
    }

    public Task<IReadOnlyList<AssessmentSummaryResponse>> Handle(GetAssessmentsQueryV2 request, CancellationToken cancellationToken)
    {
        var assessments = _dataContext.Assessments
            .Where(a => request.Requester.OrgMemberships.Contains(a.OrganizationId) || request.Requester.Roles.Contains("Admin"))
            .OrderByDescending(a => a.AssessmentYear)
            .ThenBy(a => a.Name)
            .Select(MapSummary)
            .ToList();

        return Task.FromResult<IReadOnlyList<AssessmentSummaryResponse>>(assessments);
    }

    public async Task<AssessmentSummaryResponse> Handle(CreateAssessmentCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        AccessGuards.EnsureOrgMember(request.Requester, request.Request.OrgId);

        var exists = _dataContext.Assessments.Any(a =>
            a.OrganizationId == request.Request.OrgId &&
            a.AssessmentYear == request.Request.AssessmentYear &&
            a.Name == request.Request.Name);

        if (exists)
        {
            throw new InvalidOperationException("An assessment with the same org/year/name already exists.");
        }

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.Request.OrgId,
            QuestionnaireId = request.Request.QuestionnaireId,
            AssessmentYear = request.Request.AssessmentYear,
            Name = request.Request.Name,
            CreatedByUserId = request.Requester.UserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Status = AssessmentStatus.Draft,
            ShowScoresToRespondents = false
        };

        _dataContext.Add(assessment);
        await _dataContext.SaveChangesAsync(cancellationToken);
        return MapSummary(assessment);
    }

    public Task<AssessmentDetailForConsultantResponse> Handle(GetAssessmentForConsultantQuery request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);

        var overallScore = _dataContext.AssessmentScores
            .FirstOrDefault(s => s.AssessmentId == assessment.Id && s.ScopeType == "Overall");

        return Task.FromResult(new AssessmentDetailForConsultantResponse(
            MapSummary(assessment),
            overallScore?.Percent,
            overallScore?.Maturity0To5));
    }

    public Task<AssessmentDetailForRespondentResponse> Handle(GetAssessmentForRespondentQuery request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureRespondent(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        EnsureParticipant(request.Requester.UserId, assessment.Id);
        return Task.FromResult(new AssessmentDetailForRespondentResponse(MapSummary(assessment)));
    }

    public async Task<AssessmentSummaryResponse> Handle(UpdateAssessmentStatusCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);

        var targetStatus = Enum.Parse<AssessmentStatus>(request.Request.Status, true);
        EnsureValidTransition(assessment.Status, targetStatus);

        if (targetStatus == AssessmentStatus.Finalized)
        {
            var hasOpenEvidenceRequests = _dataContext.EvidenceRequests.Any(r => r.AssessmentId == assessment.Id && r.Status == "Open");
            var hasPendingScanFiles = _dataContext.EvidenceFiles.Any(f => f.AssessmentId == assessment.Id && f.VirusScanStatus == "PendingScan");
            if (hasOpenEvidenceRequests || hasPendingScanFiles)
            {
                throw new InvalidOperationException("Cannot finalize assessment with open evidence requests or pending scans.");
            }
        }

        assessment.Status = targetStatus;
        await _dataContext.SaveChangesAsync(cancellationToken);
        return MapSummary(assessment);
    }

    public async Task<int> Handle(InviteParticipantsCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        if (!string.Equals(request.Request.Role, "ClientRespondent", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only ClientRespondent invitations are allowed.");
        }

        var invitedCount = 0;
        foreach (var email in request.Request.Emails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var user = _dataContext.Users.FirstOrDefault(u => u.Email == email);
            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    AadObjectId = $"invited:{Guid.NewGuid():N}",
                    Name = email,
                    Email = email
                };
                _dataContext.Add(user);
            }

            if (!_dataContext.OrganizationMemberships.Any(m => m.OrganizationId == assessment.OrganizationId && m.UserId == user.Id && m.Role == "ClientRespondent"))
            {
                _dataContext.Add(new OrganizationMembership
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = assessment.OrganizationId,
                    UserId = user.Id,
                    Role = "ClientRespondent"
                });
            }

            if (!_dataContext.AssessmentParticipants.Any(p => p.AssessmentId == assessment.Id && p.UserId == user.Id && p.Role == "ClientRespondent"))
            {
                _dataContext.Add(new AssessmentParticipant
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessment.Id,
                    UserId = user.Id,
                    Role = "ClientRespondent"
                });
            }

            _dataContext.Add(new AssessmentInvitation
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessment.Id,
                Email = email,
                Role = "ClientRespondent"
            });

            invitedCount++;
        }

        await _dataContext.SaveChangesAsync(cancellationToken);
        return invitedCount;
    }

    public Task<IReadOnlyList<ResponseListForConsultant>> Handle(GetResponsesForConsultantQuery request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);

        var responses = _dataContext.AssessmentResponses
            .Where(r => r.AssessmentId == assessment.Id)
            .Select(r => new ResponseListForConsultant(
                r.QuestionId,
                r.Level,
                r.Comment,
                r.Confidence,
                _dataContext.EvidenceFiles.Count(f => f.AssessmentId == assessment.Id && f.QuestionId == r.QuestionId),
                r.Score))
            .ToList();

        return Task.FromResult<IReadOnlyList<ResponseListForConsultant>>(responses);
    }

    public Task<IReadOnlyList<ResponseListForRespondent>> Handle(GetResponsesForRespondentQuery request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureRespondent(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        EnsureParticipant(request.Requester.UserId, assessment.Id);

        var responses = _dataContext.AssessmentResponses
            .Where(r => r.AssessmentId == assessment.Id && r.RespondentUserId == request.Requester.UserId)
            .Select(r => new ResponseListForRespondent(
                r.QuestionId,
                r.Level,
                r.Comment,
                r.Confidence,
                _dataContext.EvidenceFiles.Count(f => f.AssessmentId == assessment.Id && f.QuestionId == r.QuestionId)))
            .ToList();

        return Task.FromResult<IReadOnlyList<ResponseListForRespondent>>(responses);
    }

    public async Task<int> Handle(BulkUpsertResponsesCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureRespondent(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        EnsureParticipant(request.Requester.UserId, assessment.Id);

        var validQuestionIds = GetAssessmentQuestionIds(assessment);
        var upserted = 0;

        foreach (var item in request.Request.Items)
        {
            if (!validQuestionIds.Contains(item.QuestionId))
            {
                continue;
            }

            var existing = _dataContext.AssessmentResponses
                .FirstOrDefault(r => r.AssessmentId == assessment.Id && r.QuestionId == item.QuestionId && r.RespondentUserId == request.Requester.UserId);

            if (existing is null)
            {
                existing = new AssessmentResponse
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = assessment.Id,
                    QuestionId = item.QuestionId,
                    RespondentUserId = request.Requester.UserId
                };
                _dataContext.Add(existing);
            }

            existing.Level = item.Level;
            existing.Comment = item.Comment;
            existing.Confidence = item.Confidence;
            existing.Score = null;
            upserted++;
        }

        await _dataContext.SaveChangesAsync(cancellationToken);
        return upserted;
    }

    public async Task<ResponseListForRespondent> Handle(UpsertResponseCommand request, CancellationToken cancellationToken)
    {
        var bulk = new BulkUpsertResponsesRequest(new[] { request.Request with { QuestionId = request.QuestionId } });
        await Handle(new BulkUpsertResponsesCommand(request.Requester, request.AssessmentId, bulk), cancellationToken);

        var updated = _dataContext.AssessmentResponses
            .First(r => r.AssessmentId == request.AssessmentId && r.QuestionId == request.QuestionId && r.RespondentUserId == request.Requester.UserId);

        return new ResponseListForRespondent(
            updated.QuestionId,
            updated.Level,
            updated.Comment,
            updated.Confidence,
            _dataContext.EvidenceFiles.Count(f => f.AssessmentId == request.AssessmentId && f.QuestionId == request.QuestionId));
    }

    public async Task<EvidenceRequestDto> Handle(CreateEvidenceRequestCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        var validQuestionIds = GetAssessmentQuestionIds(assessment);

        if (!validQuestionIds.Contains(request.Request.QuestionId))
        {
            throw new InvalidOperationException("Question does not belong to assessment questionnaire.");
        }

        var entity = new EvidenceRequest
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment.Id,
            QuestionId = request.Request.QuestionId,
            DueDate = request.Request.DueDate,
            Notes = request.Request.Notes,
            Status = "Open",
            CreatedByUserId = request.Requester.UserId
        };

        _dataContext.Add(entity);
        await _dataContext.SaveChangesAsync(cancellationToken);
        return new EvidenceRequestDto(entity.Id, entity.QuestionId, entity.Status, entity.DueDate, entity.Notes);
    }

    public Task<IReadOnlyList<EvidenceRequestDto>> Handle(GetEvidenceRequestsQuery request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);

        var requests = _dataContext.EvidenceRequests
            .Where(e => e.AssessmentId == assessment.Id)
            .Select(e => new EvidenceRequestDto(e.Id, e.QuestionId, e.Status, e.DueDate, e.Notes))
            .ToList();

        return Task.FromResult<IReadOnlyList<EvidenceRequestDto>>(requests);
    }

    public async Task<EvidenceRequestDto> Handle(UpdateEvidenceRequestStatusCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        var entity = _dataContext.EvidenceRequests.FirstOrDefault(r => r.Id == request.RequestId && r.AssessmentId == assessment.Id)
            ?? throw new NotFoundException("Evidence request not found.");

        entity.Status = request.Request.Status;
        entity.Notes = request.Request.Notes;
        await _dataContext.SaveChangesAsync(cancellationToken);

        return new EvidenceRequestDto(entity.Id, entity.QuestionId, entity.Status, entity.DueDate, entity.Notes);
    }

    public async Task<CreateEvidenceUploadResponse> Handle(CreateEvidenceUploadCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureRespondent(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        EnsureParticipant(request.Requester.UserId, assessment.Id);

        var evidenceRequest = _dataContext.EvidenceRequests.FirstOrDefault(r => r.Id == request.Request.EvidenceRequestId && r.AssessmentId == assessment.Id)
            ?? throw new NotFoundException("Evidence request not found.");

        if (!string.Equals(evidenceRequest.Status, "Open", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Evidence upload is only allowed for Open evidence requests.");
        }

        var sasResult = await _blobStorageService.GetUploadSasAsync(
            assessment.OrganizationId,
            assessment.Id,
            evidenceRequest.QuestionId,
            evidenceRequest.Id,
            request.Request.FileName,
            request.Request.FileType,
            request.Request.FileSizeBytes,
            cancellationToken);

        return new CreateEvidenceUploadResponse(sasResult.UploadUrl, sasResult.BlobName, sasResult.ExpiresAt);
    }

    public async Task<CompleteEvidenceUploadResponse> Handle(CompleteEvidenceUploadCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureRespondent(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        EnsureParticipant(request.Requester.UserId, assessment.Id);

        var evidenceRequest = _dataContext.EvidenceRequests.FirstOrDefault(r => r.Id == request.Request.EvidenceRequestId && r.AssessmentId == assessment.Id)
            ?? throw new NotFoundException("Evidence request not found.");

        if (!string.Equals(evidenceRequest.Status, "Open", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Evidence completion is only allowed for Open evidence requests.");
        }

        var expectedPrefix = $"evidence/{assessment.OrganizationId}/{assessment.Id}/{evidenceRequest.QuestionId}/";
        if (!request.Request.BlobName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Blob name does not match expected evidence location.");
        }

        var entity = new EvidenceFile
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment.Id,
            QuestionId = evidenceRequest.QuestionId,
            EvidenceRequestId = evidenceRequest.Id,
            UploadedByUserId = request.Requester.UserId,
            FileName = request.Request.FileName,
            FileType = request.Request.FileType,
            BlobName = request.Request.BlobName,
            VirusScanStatus = "PendingScan"
        };

        _dataContext.Add(entity);
        await _dataContext.SaveChangesAsync(cancellationToken);
        await _evidenceScanService.QueueScanAsync(entity.Id, cancellationToken);

        return new CompleteEvidenceUploadResponse(entity.Id, entity.BlobName, entity.VirusScanStatus);
    }

    public Task<ScoreSnapshotResponse> Handle(ComputeScoreCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        return Task.FromResult(_scoringService.ComputeAndPersist(assessment.Id, assessment.OrganizationId));
    }

    public Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        return Task.FromResult(_dashboardService.Build(assessment.Id, assessment.OrganizationId));
    }

    public Task<ReportResponse> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        AccessGuards.EnsureConsultant(request.Requester);
        var assessment = GetAssessmentWithOrgScope(request.Requester, request.AssessmentId);
        return Task.FromResult(_reportService.Generate(assessment.Id, assessment.OrganizationId, request.Request));
    }

    public async Task<AiGuidanceResponse> Handle(GetAiGuidanceCommand request, CancellationToken cancellationToken)
    {
        if (request.Requester.UserId == Guid.Empty)
        {
            throw new ForbiddenAccessException("Authenticated user required.");
        }

        var orgId = request.Requester.OrgMemberships.FirstOrDefault();
        return await _aiGuidanceService.GenerateAsync(request.Request, orgId, cancellationToken);
    }

    private Assessment GetAssessmentWithOrgScope(RequesterContext requester, Guid assessmentId)
    {
        var assessment = _dataContext.Assessments.FirstOrDefault(a => a.Id == assessmentId)
            ?? throw new NotFoundException("Assessment not found.");
        AccessGuards.EnsureOrgMember(requester, assessment.OrganizationId);
        return assessment;
    }

    private void EnsureParticipant(Guid userId, Guid assessmentId)
    {
        if (!_dataContext.AssessmentParticipants.Any(p => p.AssessmentId == assessmentId && p.UserId == userId && p.Role == "ClientRespondent"))
        {
            throw new ForbiddenAccessException("Respondent is not assigned to this assessment.");
        }
    }

    private HashSet<Guid> GetAssessmentQuestionIds(Assessment assessment)
    {
        var domainIds = _dataContext.Domains.Where(d => d.QuestionnaireId == assessment.QuestionnaireId).Select(d => d.Id).ToList();
        var categoryIds = _dataContext.Categories.Where(c => domainIds.Contains(c.DomainId)).Select(c => c.Id).ToList();
        return _dataContext.Questions.Where(q => categoryIds.Contains(q.CategoryId)).Select(q => q.Id).ToHashSet();
    }

    private static AssessmentSummaryResponse MapSummary(Assessment assessment)
        => new(
            assessment.Id,
            assessment.OrganizationId,
            assessment.Name,
            assessment.AssessmentYear,
            assessment.Status.ToString(),
            assessment.QuestionnaireId,
            assessment.CreatedByUserId,
            assessment.CreatedAtUtc);

    private static void EnsureValidTransition(AssessmentStatus current, AssessmentStatus target)
    {
        if (current == target)
        {
            return;
        }

        var allowed = current switch
        {
            AssessmentStatus.Draft => new[] { AssessmentStatus.InCollection },
            AssessmentStatus.InCollection => new[] { AssessmentStatus.UnderReview },
            AssessmentStatus.UnderReview => new[] { AssessmentStatus.EvidenceRequested, AssessmentStatus.Finalized },
            AssessmentStatus.EvidenceRequested => new[] { AssessmentStatus.EvidenceReceived },
            AssessmentStatus.EvidenceReceived => new[] { AssessmentStatus.UnderReview, AssessmentStatus.Finalized },
            AssessmentStatus.Finalized => Array.Empty<AssessmentStatus>(),
            _ => Array.Empty<AssessmentStatus>()
        };

        if (!allowed.Contains(target))
        {
            throw new InvalidOperationException($"Invalid status transition from {current} to {target}.");
        }
    }
}