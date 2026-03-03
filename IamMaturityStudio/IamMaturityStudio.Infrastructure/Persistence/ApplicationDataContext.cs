using IamMaturityStudio.Application.Interfaces;
using IamMaturityStudio.Domain.Entities;
using IamDomain = IamMaturityStudio.Domain.Entities.Domain;

namespace IamMaturityStudio.Infrastructure.Persistence;

public class ApplicationDataContext : IApplicationDataContext
{
    private readonly IamDbContext _dbContext;

    public ApplicationDataContext(IamDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Organization> Organizations => _dbContext.Organizations;
    public IQueryable<User> Users => _dbContext.Users;
    public IQueryable<OrganizationMembership> OrganizationMemberships => _dbContext.OrganizationMemberships;
    public IQueryable<Questionnaire> Questionnaires => _dbContext.Questionnaires;
    public IQueryable<IamDomain> Domains => _dbContext.Domains;
    public IQueryable<Category> Categories => _dbContext.Categories;
    public IQueryable<Question> Questions => _dbContext.Questions;
    public IQueryable<Assessment> Assessments => _dbContext.Assessments;
    public IQueryable<AssessmentParticipant> AssessmentParticipants => _dbContext.AssessmentParticipants;
    public IQueryable<AssessmentInvitation> AssessmentInvitations => _dbContext.AssessmentInvitations;
    public IQueryable<AssessmentResponse> AssessmentResponses => _dbContext.AssessmentResponses;
    public IQueryable<EvidenceRequest> EvidenceRequests => _dbContext.EvidenceRequests;
    public IQueryable<EvidenceFile> EvidenceFiles => _dbContext.EvidenceFiles;
    public IQueryable<AssessmentScore> AssessmentScores => _dbContext.AssessmentScores;
    public IQueryable<Report> Reports => _dbContext.Reports;
    public IQueryable<OrgScoringModel> OrgScoringModels => _dbContext.OrgScoringModels;

    public void Add<TEntity>(TEntity entity) where TEntity : class => _dbContext.Set<TEntity>().Add(entity);

    public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        => _dbContext.Set<TEntity>().AddRange(entities);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}