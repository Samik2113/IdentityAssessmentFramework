using IamMaturityStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using IamDomainEntity = IamMaturityStudio.Domain.Entities.Domain;

namespace IamMaturityStudio.Infrastructure.Persistence;

public class IamDbContext : DbContext
{
    public IamDbContext(DbContextOptions<IamDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Questionnaire> Questionnaires => Set<Questionnaire>();
    public DbSet<IamDomainEntity> Domains => Set<IamDomainEntity>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentParticipant> AssessmentParticipants => Set<AssessmentParticipant>();
    public DbSet<AssessmentInvitation> AssessmentInvitations => Set<AssessmentInvitation>();
    public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();
    public DbSet<EvidenceRequest> EvidenceRequests => Set<EvidenceRequest>();
    public DbSet<EvidenceFile> EvidenceFiles => Set<EvidenceFile>();
    public DbSet<AssessmentScore> AssessmentScores => Set<AssessmentScore>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OrgScoringModel> OrgScoringModels => Set<OrgScoringModel>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Questionnaire>()
            .HasIndex(q => new { q.Name, q.Version })
            .IsUnique();

        modelBuilder.Entity<IamDomainEntity>()
            .HasIndex(d => new { d.QuestionnaireId, d.Code })
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.DomainId, c.Code })
            .IsUnique();

        modelBuilder.Entity<Question>()
            .HasIndex(q => new { q.CategoryId, q.Code })
            .IsUnique();

        modelBuilder.Entity<OrgScoringModel>()
            .HasIndex(m => new { m.OrganizationId, m.Name })
            .IsUnique();

        modelBuilder.Entity<OrganizationMembership>()
            .HasIndex(m => new { m.OrganizationId, m.UserId, m.Role })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.AadObjectId)
            .IsUnique();

        modelBuilder.Entity<Assessment>()
            .HasIndex(a => new { a.OrganizationId, a.AssessmentYear, a.Name })
            .IsUnique();

        modelBuilder.Entity<AssessmentParticipant>()
            .HasIndex(p => new { p.AssessmentId, p.UserId, p.Role })
            .IsUnique();

        modelBuilder.Entity<AssessmentResponse>()
            .HasIndex(r => new { r.AssessmentId, r.QuestionId, r.RespondentUserId })
            .IsUnique();

        // TODO: Configure navigation properties and richer constraints.
    }
}