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
    public DbSet<AssessmentResponse> AssessmentResponses => Set<AssessmentResponse>();
    public DbSet<EvidenceRequest> EvidenceRequests => Set<EvidenceRequest>();
    public DbSet<EvidenceFile> EvidenceFiles => Set<EvidenceFile>();
    public DbSet<AssessmentScore> AssessmentScores => Set<AssessmentScore>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OrgScoringModel> OrgScoringModels => Set<OrgScoringModel>();

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

        // TODO: Configure navigation properties and richer constraints.
    }
}