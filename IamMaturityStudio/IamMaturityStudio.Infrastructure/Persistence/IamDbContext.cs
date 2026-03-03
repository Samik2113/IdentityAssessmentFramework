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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TODO: Configure entity relationships, constraints, and indexes.
        // TODO: Add value object conversions and precision rules as needed.
    }
}