using IamMaturityStudio.Domain.Entities;
using IamDomain = IamMaturityStudio.Domain.Entities.Domain;
using IamMaturityStudio.Application.Contracts;

namespace IamMaturityStudio.Application.Interfaces;

public interface IApplicationDataContext
{
    IQueryable<Organization> Organizations { get; }
    IQueryable<User> Users { get; }
    IQueryable<OrganizationMembership> OrganizationMemberships { get; }
    IQueryable<Questionnaire> Questionnaires { get; }
    IQueryable<IamDomain> Domains { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<Question> Questions { get; }
    IQueryable<Assessment> Assessments { get; }
    IQueryable<AssessmentParticipant> AssessmentParticipants { get; }
    IQueryable<AssessmentInvitation> AssessmentInvitations { get; }
    IQueryable<AssessmentResponse> AssessmentResponses { get; }
    IQueryable<EvidenceRequest> EvidenceRequests { get; }
    IQueryable<EvidenceFile> EvidenceFiles { get; }
    IQueryable<AssessmentScore> AssessmentScores { get; }
    IQueryable<Report> Reports { get; }
    IQueryable<OrgScoringModel> OrgScoringModels { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IStorageSasService
{
    string CreateUploadUrl(string blobName, TimeSpan lifetime);
}

public interface IScoringService
{
    ScoreSnapshotResponse ComputeAndPersist(Guid assessmentId, Guid orgId);
}

public interface IDashboardService
{
    DashboardResponse Build(Guid assessmentId, Guid orgId);
}

public interface IReportService
{
    ReportResponse Generate(Guid assessmentId, Guid orgId, GenerateReportRequest request);
}

public interface IAiGuidanceService
{
    AiGuidanceResponse Generate(AiGuidanceRequest request);
}